Imports System.IO
Imports System.Runtime.InteropServices.ComTypes
Imports System.Text.RegularExpressions
Imports System.Windows
Module UI_GeneratorMain
    Private Function trimQuotes(line As String)
        While InStr(line, """")
            Dim startq = InStr(line, """")
            Dim endq = InStr(line.Substring(startq), """")
            line = line.Substring(0, startq - 1) + line.Substring(endq + startq)
        End While
        Return line
    End Function
    Private Function isAlpha(ByVal letterChar As String) As Boolean
        Return Regex.IsMatch(letterChar, "^[A-Za-z]{1}$")
    End Function
    Private Function SortByDate(X As FileInfo, Y As FileInfo) As Integer
        Return X.LastWriteTime.CompareTo(Y.LastWriteTime)
    End Function
    Private Function checkDates(dirInfo As DirectoryInfo, algorithmGroupNames As FileInfo) As Boolean
        For Each fileInfo As FileInfo In dirInfo.GetFiles()
            If fileInfo.Name = "VB_Common.vb" Then Continue For
            If fileInfo.Name = "VB_Parent.vb" Then Continue For
            If fileInfo.Name = "VB_Task.vb" Then Continue For
            If fileInfo.Name = "VB_Externs.vb" Then Continue For
            If fileInfo.Name.StartsWith("Options") Then Continue For
            If fileInfo.Name = "AlgorithmList.vb" Then Continue For
            Dim result As Integer = DateTime.Compare(fileInfo.LastWriteTime, algorithmGroupNames.LastWriteTime)
            If result > 0 Then Return True
        Next
        Return False
    End Function
    Sub Main()
        Dim CSnames As New SortedList(Of String, String)
        Dim OpenGLnames As New SortedList(Of String, String)
        Dim PYnames As New SortedList(Of String, String)
        Dim VBNames As New SortedList(Of String, String)
        Dim allButPython As New SortedList(Of String, String)
        Dim PYStreamNames As New SortedList(Of String, String)
        Dim LastEdits As New SortedList(Of String, String)

        Dim cppAlgorithmInput = New FileInfo("../CPP_Classes/CPP_Algorithms.h")
        Dim CPPIncludeOnly = New FileInfo("../CPP_Classes/CPP_AI_Generated.h")
        Dim CSInputs = {New FileInfo("../CS_Classes/AI_Gen.cs").FullName,
                        New FileInfo("../CS_Classes/Non_AI.cs").FullName}
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../VB_classes/")
        If cppAlgorithmInput.Exists = False Then
            cppAlgorithmInput = New FileInfo("../../CPP_Classes/CPP_Algorithms.h")
            CPPIncludeOnly = New FileInfo("../../CPP_Classes/CPP_AI_Generated.h")
            CSInputs = {New FileInfo("../../CS_Classes/AI_Gen.cs").FullName,
                        New FileInfo("../../CS_Classes/Non_AI.cs").FullName}
            VBcodeDir = New DirectoryInfo(CurDir() + "/../../VB_classes/")
        End If
        Dim HomeDir As New DirectoryInfo(VBcodeDir.FullName + "/../")

        Dim indexTestFile = New FileInfo(HomeDir.FullName + "/Data/AlgorithmGroupNames.txt")
#If DEBUG Then
        If indexTestFile.Exists Then My.Computer.FileSystem.DeleteFile(indexTestFile.FullName)
#End If
        If indexTestFile.Exists And Not Debugger.IsAttached Then
            If checkDates(New DirectoryInfo(HomeDir.FullName + "/CS_Classes/"), indexTestFile) = False Then
                If checkDates(New DirectoryInfo(HomeDir.FullName + "/VB_Classes/"), indexTestFile) = False Then
                    Console.WriteLine("The user interface is already up to date.")
                    Exit Sub ' nothing to trigger 
                End If
            End If
        End If
        Console.WriteLine("Starting work to generate the user interface.")

        Dim includeOnly = File.ReadAllLines(CPPIncludeOnly.FullName)
        Dim cppLines As Integer, csLines As Integer
        For Each incline In includeOnly
            incline = Trim(incline)
            If incline.StartsWith("//") Then Continue For
            If incline.Length = 0 Then Continue For
            cppLines += 1
        Next

        For Each csFile In CSInputs
            Dim CSAlgorithms = File.ReadAllLines(csFile)
            For Each algline In CSAlgorithms
                algline = Trim(algline)
                If algline = "{" Or algline = "}" Then Continue For
                If algline.StartsWith("//") Then Continue For
                If algline.Length = 0 Then Continue For
                csLines += 1
            Next
        Next

        ' first read all the cpp functions that are present in the project
        Dim functionInput As New FileInfo(HomeDir.FullName + "/CPP_Classes/CPP_FunctionNames.h")
        Dim srFunctions = New StreamReader(functionInput.FullName)
        Dim cppFunctionNames As New SortedList(Of String, String)
        Dim unsortedFunctions As New List(Of String)
        While srFunctions.EndOfStream = False
            Dim cppline = srFunctions.ReadLine()
            If cppline.Contains("enum functions") Then
                While 1
                    cppline = Trim(srFunctions.ReadLine())
                    If cppline = "{" Then Continue While
                    If cppline = "};" Then Exit While
                    Dim split = cppline.Split(",")
                    If split(0).Contains("MAX_FUNCTION") Then Continue While
                    cppFunctionNames.Add(split(0).Substring(0), split(0))
                    unsortedFunctions.Add(split(0).Substring(0))
                End While
            End If
        End While
        srFunctions.Close()

        Dim fileNames As New List(Of String)
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim pythonAppDir As New IO.DirectoryInfo(HomeDir.FullName + "/Python_Classes/")

        Dim vbProjFile As New FileInfo(HomeDir.FullName + "/VB_Classes/VB_Classes.vbproj")
        Dim readVBProj = New StreamReader(vbProjFile.FullName)
        While readVBProj.EndOfStream = False
            Dim vbline = readVBProj.ReadLine()
            If Trim(vbline).StartsWith("<Compile Include=") Then
                If InStr(vbline, ".vb""") Then
                    Dim startname = InStr(vbline, "=") + 2
                    vbline = Mid(vbline, startname)
                    Dim endName = InStr(vbline, """")
                    vbline = Mid(vbline, 1, endName - 1)
                    If vbline.Contains("AlgorithmList.vb") = False And vbline.Contains("My Project") = False Then
                        fileNames.Add(VBcodeDir.FullName + vbline)
                    End If
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
        Dim CodeLineCount As Integer = cppLines + csLines ' now adding in the C++ and C# lines...
        Dim sortedNames As New SortedList(Of String, Integer)
        For Each fileName In fileNames
            If fileName.EndsWith(".py") And fileName.Contains("__init") = False Then
                Dim fileinfo As New FileInfo(fileName)
                If sortedNames.Keys.Contains(fileinfo.Name) = False Then sortedNames.Add(fileinfo.Name, sortedNames.Count)
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("VB_Parent.vb") = False And fileName.EndsWith("createalgorithms.cs") = False Then
                    Dim nextFile As New System.IO.StreamReader(fileName)
                    While nextFile.Peek() <> -1
                        Dim fileline = Trim(nextFile.ReadLine())
                        fileline = Replace(fileline, vbTab, "")
                        If fileline IsNot Nothing Then
                            If fileline.Substring(0, 1) <> "'" Then
                                If Len(fileline) > 0 Then CodeLineCount += 1
                                If LCase(fileline).StartsWith("public class") Then
                                    Dim split As String() = Regex.Split(fileline, "\W+")
                                    If fileline.EndsWith(" : Inherits VB_Parent") Then className = split(2)
                                End If
                                If LCase(fileline).StartsWith("public sub new(") And
                                    sortedNames.ContainsKey(className) = False Then
                                    If sortedNames.Keys.Contains(className) = False Then sortedNames.Add(className, sortedNames.Count)
                                End If
                            End If
                        End If
                    End While
                End If
            End If
        Next

        Dim csSortedNames As New SortedList(Of String, Integer)
        Dim csFileNames As New List(Of String)
        Dim csAdds As New List(Of String)
        ' we only want python files that are included in the Python_Classes Project.  Other Python files may be support modules or just experiments.
        Dim csAppDir As New IO.DirectoryInfo(HomeDir.FullName + "/CS_Classes/")
        Dim csFiles() As String = Directory.GetFiles(csAppDir.FullName, "*.cs", SearchOption.AllDirectories)
        For Each csFile As String In csFiles
            csFileNames.Add(csFile)
        Next
        For Each fileName In CSInputs
            If fileName.EndsWith("CS_Parent.cs") = False Then
                Dim csName As String = ""
                Dim nextFile As New System.IO.StreamReader(fileName)
                While nextFile.Peek() <> -1
                    Dim csline = Trim(nextFile.ReadLine())
                    If csline Is Nothing Then Continue While
                    If csline.Length > 1 Then
                        If csline.Substring(0, 2) <> "//" Then
                            CodeLineCount += 1
                            If LCase(csline).StartsWith("public class ") Then
                                Dim split As String() = Regex.Split(csline, "\W+")
                                If csline.EndsWith(" : CS_Parent") Then
                                    csName = split(2)
                                    If csAdds.Contains(fileName) = False Then
                                        csAdds.Add(fileName)
                                        fileNames.Add(fileName)
                                    End If
                                End If
                            End If
                            If LCase(csline).StartsWith("public ") And csSortedNames.ContainsKey(csName) = False And
                                csName <> "" Then
                                csSortedNames.Add(csName, csSortedNames.Count)
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
        Dim CSlistInfo As New FileInfo(HomeDir.FullName + "\CS_Classes\createAlgorithms.cs")
        Dim sw As New StreamWriter(CSlistInfo.FullName)
        sw.WriteLine("// this file is automatically generated in a pre-build step.  Any manual modifications will be lost.")
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
        sw.WriteLine(vbTab + vbTab + vbTab + "return new AddWeighted_Basics_CS(task);")
        sw.WriteLine(vbTab + vbTab + "}")
        sw.WriteLine(vbTab + "}")
        sw.WriteLine("}")
        sw.Close()





        Dim listInfo As New FileInfo(HomeDir.FullName + "/UI_Generator/AlgorithmList.vb")
        sw = New StreamWriter(listInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Do not waste your time modifying manually.")
        sw.WriteLine("Public Class AlgorithmList")
        sw.WriteLine("Public Enum cppFunctionNames")
        For i = 0 To unsortedFunctions.Count - 1
            sw.WriteLine(unsortedFunctions(i))
        Next
        sw.WriteLine("End Enum")

        sw.WriteLine("Public Function createVBAlgorithm( algorithmName as string) As VB_Parent")
        sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        For i = 0 To cleanNames.Count - 1
            Dim nextName = cleanNames(i)
            If nextName.EndsWith(".py") = False Then
                sw.WriteLine(vbTab + "if algorithmName = """ + nextName + """ Then return new " + nextName)
            End If

            If nextName.StartsWith("CPP_Basics") Then
                For j = 0 To cppFunctionNames.Count - 1
                    Dim functionText = cppFunctionNames.ElementAt(j).Key
                    Dim func = functionText
                    functionText = functionText.Substring(1)
                    sw.WriteLine("if algorithmName = """ + functionText + """ Then return new CPP_Basics(cppFunctionNames." + func + ")")
                Next
            End If
        Next

        sw.WriteLine("return nothing")
        sw.WriteLine("End Function")
        sw.WriteLine("End Class")
        sw.Close()



        Dim apiList As New List(Of String)
        Dim apiListLCase As New List(Of String)

        ' read the list of OpenCV API's we will be looking for
        Dim srAPI = New System.IO.StreamReader(HomeDir.FullName + "\Data\OpenCVapi.txt")
        While srAPI.EndOfStream = False
            Dim apiline = srAPI.ReadLine()
            If apiline <> "" Then
                apiListLCase.Add(LCase(apiline) + "(") ' it needs the parenthesis to make sure it is a function.
                apiList.Add(apiline + "(") ' it needs the parenthesis to make sure it is a function.
            End If
        End While
        srAPI.Close()



        Dim textInfo As New FileInfo(HomeDir.FullName + "/Data/AlgorithmList.txt")
        sw = New StreamWriter(textInfo.FullName)
        sw.WriteLine("CodeLineCount = " + CStr(CodeLineCount))
        For i = 0 To cleanNames.Count - 1
            If cleanNames(i).StartsWith("CSV_Basics") Then
                For j = 0 To csSortedNames.Count - 1
                    sw.WriteLine(csSortedNames.ElementAt(j).Key)
                    allButPython.Add(csSortedNames.ElementAt(j).Key, csSortedNames.ElementAt(j).Key)
                Next
                sw.WriteLine(cleanNames(i))
            ElseIf cleanNames(i).StartsWith("CPP_Basics") Then ' skip writing CPP_Basics but write all the others...
                For j = 0 To cppFunctionNames.Count - 1
                    Dim functionText = cppFunctionNames.ElementAt(j).Key
                    sw.WriteLine(functionText.Substring(1))
                    allButPython.Add(functionText.Substring(1), functionText.Substring(1))
                Next
            Else
                sw.WriteLine(cleanNames(i))
            End If

            If cleanNames(i).EndsWith(".py") Then
                PYnames.Add(cleanNames(i), cleanNames(i))
                If cleanNames(i).EndsWith("_PS.py") Then PYStreamNames.Add(cleanNames(i), cleanNames(i))
            Else
                If cleanNames(i) <> "" Then
                    If cleanNames(i).Contains("Python_Stream") = False And cleanNames(i).Contains("Python") = False And
                        cleanNames(i).Contains("CPP_Basics") = False Then
                        VBNames.Add(cleanNames(i), cleanNames(i))
                        apiList.Add(cleanNames(i))
                        apiListLCase.Add(LCase(cleanNames(i)))
                        allButPython.Add(cleanNames(i), cleanNames(i))
                    End If
                End If
            End If
        Next
        sw.Close()




        Dim vbDir As New DirectoryInfo(HomeDir.FullName + "/VB_Classes/")
        Dim fileList As List(Of FileInfo) = vbDir.GetFiles().ToList()
        fileList.Sort(AddressOf SortByDate)
        Dim filesByDate As New List(Of String)
        For Each entry In fileList
            If entry.Name.EndsWith(".vb") Then filesByDate.Add(entry.Name)
        Next



        Dim tokens(apiList.Count - 1) As String
        For Each fileName In fileNames
            Dim info = New FileInfo(fileName)

            Dim dateIndex = filesByDate.IndexOf(info.Name)
            dateIndex = filesByDate.Count - dateIndex

            Dim nextFile As New System.IO.StreamReader(info.FullName)
            className = ""
            If info.Name.EndsWith(".py") Then className = info.Name ' python file names are the class name - they don't have multiple classnames per file
            While nextFile.Peek() <> -1
                Dim codeline = Trim(nextFile.ReadLine())
                If codeline.Trim.StartsWith("//") Then Continue While
                If codeline.Contains("_CS ") Then
                    If codeline.Contains("public class ") Then
                        Dim split = codeline.Split(" \W+")
                        If CSnames.Keys.Contains(split(2)) Then
                            MsgBox("There is a duplicate name in the C# code!" + vbCrLf + "Duplicate is " + split(2) +
                               vbCrLf + "Terminating the generation of the UI...")
                            End
                        End If
                        CSnames.Add(split(2), split(2))
                    End If
                End If
                Dim lcaseLine = " " + LCase(codeline)
                If codeline = "" Or Trim(codeline).StartsWith("'") Or Trim(codeline).StartsWith("#") Then Continue While
                If LCase(codeline).StartsWith("public class") And LCase(codeline).EndsWith("inherits vb_parent") Then
                    Dim split As String() = Regex.Split(codeline, "\W+")
                    className = split(2) ' public class <classname>
                    If className.StartsWith("Python_") Then PYnames.Add(className, className)
                    If className.EndsWith("_PS.py") Then PYStreamNames.Add(className, className)
                    If className.Contains("OpenGL") Then OpenGLnames.Add(className, className)
                    Continue While
                End If
                If className <> "" Then
                    For i = 0 To apiList.Count - 1
                        Dim index = InStr(lcaseLine, apiListLCase(i))
                        If index > 0 Then
                            If isAlpha(lcaseLine.Substring(index - 2, 1)) = False Then
                                If tokens(i) Is Nothing Then
                                    tokens(i) = className
                                Else
                                    If tokens(i).Contains(className) = False Then tokens(i) += "," + className
                                End If
                            End If
                        End If
                    Next
                End If
            End While
        Next



        ' add the VB Class names to each entry in tokens.
        For i = 0 To tokens.Count - 1
            If apiList(i).EndsWith("(") = False Then
                If tokens(i) Is Nothing Then tokens(i) = apiList(i) Else tokens(i) += "," + apiList(i)
            End If
        Next

        Dim sortedAPIs As New SortedList(Of String, String)
        For i = 0 To tokens.Count - 1
            If tokens(i) IsNot Nothing Then
                If apiList(i).EndsWith("(") Then apiList(i) = apiList(i).Substring(0, Len(apiList(i)) - 1)
                ' sort the tokens before creating the final entry
                Dim split As String() = Regex.Split(tokens(i), ",")
                Dim tokenSort As New SortedList(Of String, String)
                For j = 0 To split.Length - 1
                    If tokenSort.ContainsKey(split(j)) = False Then tokenSort.Add(split(j), split(j)) ' the duplicates come from adding self to classname above.
                Next
                Dim finalEntry = tokenSort.ElementAt(0).Key
                For j = 1 To tokenSort.Count - 1
                    finalEntry += "," + tokenSort.ElementAt(j).Key
                Next

                If apiList(i).StartsWith("cv.cv2.") Then
                    apiList(i) = apiList(i).Substring(7) ' + "(OpenCV version)"
                    If apiList(i).StartsWith("max") Then apiList(i) += "(OpenCV version)"
                    If apiList(i).StartsWith("min") Then apiList(i) += "(OpenCV version)"
                End If
                sortedAPIs.Add(apiList(i), finalEntry)
            End If
        Next

        'Dim count As Integer
        'Dim testKeys = New List(Of String)
        'For Each nm In CSnames.Keys
        '    testKeys.Add(nm.Substring(0, nm.Length - 3))
        'Next
        'For Each nm In VBNames.Keys
        '    If testKeys.Contains(nm) = False And nm.StartsWith("Options_") = False Then
        '        Console.WriteLine("missing from C# code: " + nm)
        '        count += 1
        '    End If
        'Next

        Dim dataDir As New FileInfo(HomeDir.FullName + "/Data/")
        sw = New StreamWriter(dataDir.FullName + "AlgorithmGroupNames.txt")
        Dim allCount = allButPython.Count + PYnames.Count
        sw.WriteLine("<All (" + CStr(allCount) + ")>")

        sw.Write("<All but Python (" + CStr(allButPython.Count) + ")>")
        For i = 0 To allButPython.Count - 1
            sw.Write("," + allButPython.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<All C# (" + CStr(CSnames.Count) + ")>")
        For i = 0 To CSnames.Count - 1
            sw.Write("," + CSnames.ElementAt(i).Key)
        Next
        sw.WriteLine()


        Dim cppNames As New List(Of String)
        For Each nm In allButPython.Keys
            If nm.Contains("CPP_") Then cppNames.Add(nm)
        Next

        sw.Write("<All C++ (" + CStr(cppNames.Count) + ")>")
        For i = 0 To cppNames.Count - 1
            sw.Write("," + cppNames(i))
        Next
        sw.WriteLine()

        sw.Write("<All OpenGL (" + CStr(OpenGLnames.Count) + ")>")
        For i = 0 To OpenGLnames.Count - 1
            sw.Write("," + OpenGLnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<All PyStream(" + CStr(PYStreamNames.Count) + ">")
        For i = 0 To PYStreamNames.Count - 1
            sw.Write("," + PYStreamNames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<All Python (" + CStr(PYnames.Count) + ")>")
        For i = 0 To PYnames.Count - 1
            sw.Write("," + PYnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<All VB.Net (" + CStr(VBNames.Count) + ")>")
        For i = 0 To VBNames.Count - 1
            sw.Write("," + VBNames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        'sw.WriteLine("<All using recorded data>")

        For i = 0 To sortedAPIs.Count - 1
            Dim token = sortedAPIs.ElementAt(i)
            sw.WriteLine(token.Key + "," + sortedAPIs.ElementAt(i).Value)
        Next
        sw.Close()

        Dim rankList As New FileInfo(dataDir.FullName + "RankList.txt")
        Dim rankingInput As New FileInfo(dataDir.FullName + "/AlgorithmGroupNames.txt")
        Dim sr = New StreamReader(rankingInput.FullName)
        Dim code As String = sr.ReadToEnd
        sr.Close()
        Dim lines() As String = code.Split(vbLf)
        Dim algorithms As New SortedList(Of String, Integer)
        Dim maxCount As Integer = Integer.MinValue
        For i = 0 To lines.Count - 1
            lines(i) = lines(i).Trim
            Dim nextLine = lines(i)
            If nextLine.Length = 0 Then Continue For
            If nextLine.StartsWith("<") = False Then
                Dim split() = nextLine.Split(",")
                If split(0).StartsWith(split(0).Substring(0, 1).ToUpper()) Then
                    If split(0).Contains("_") And split.Length > 3 Then
                        Dim nameCount = split.Length - 2
                        If maxCount < nameCount Then maxCount = nameCount
                        algorithms.Add(split(0), split.Length - 2)
                    End If
                End If
            End If
        Next

        Dim reusedList As New SortedList(Of String, String)
        For Each alg In algorithms
            Dim nextAlg = alg.Key
            For Each line In lines
                If line.Length = 0 Then Continue For
                If line.StartsWith(nextAlg) Then
                    Dim callees = line.Split(",")
                    For Each func In callees
                        If reusedList.ContainsKey(func) = False Then reusedList.Add(func, func)
                    Next
                End If
            Next
        Next


        Dim buildReusedList As String = "<All Reused and Callees>"
        For Each func In reusedList
            If func.Key.StartsWith("") = False Then buildReusedList += "," + func.Key
        Next

        Dim algorithmRank As New SortedList(Of String, String)

        For i = 0 To algorithms.Count - 1
            Dim name = algorithms.ElementAt(i).Key
            Dim key = Format(algorithms.ElementAt(i).Value, "0000") + name
            algorithmRank.Add(key, name)
        Next

        sw = New StreamWriter(rankList.FullName)
        Dim rankEntries As String = ""
        Dim rank As Integer
        For i = 0 To algorithmRank.Count - 1
            Dim entry = algorithmRank.ElementAt(i).Key
            rank = CInt(entry.Substring(0, 4))
            Static saveRank = rank
            If rank <> saveRank Then
                sw.WriteLine("<Reuse Rank " + CStr(saveRank) + " times>" + rankEntries)
                saveRank = rank
                rankEntries = ""
            End If
            rankEntries += "," + algorithmRank.ElementAt(i).Value
        Next
        If rankEntries.Length > 0 Then sw.WriteLine("<Reuse Rank " + CStr(rank) + " times>" + rankEntries)
        sw.Close()

        Dim swAll As New StreamWriter(rankingInput.FullName)
        Dim saveIndex As Integer
        For saveIndex = 0 To lines.Count - 1
            swAll.WriteLine(lines(saveIndex))
            If lines(saveIndex).Contains("<PyStream>") Then Exit For
            If lines(saveIndex).Contains("<All VB.Net>") Then
                swAll.WriteLine(buildReusedList)
            End If
        Next

        sr = New StreamReader(rankList.FullName)
        While sr.EndOfStream = False
            swAll.WriteLine(sr.ReadLine)
        End While
        sr.Close()

        For i = saveIndex + 1 To lines.Count - 1
            swAll.WriteLine(lines(i))
        Next
        swAll.Close()
    End Sub
End Module
