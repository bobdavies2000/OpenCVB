Imports System.IO
Imports System.Runtime.InteropServices.ComTypes
Imports System.Text.RegularExpressions
Imports System.Windows
Module UI_GeneratorMain
    Sub Main()
        Dim CSnames As New SortedList(Of String, String)
        Dim OpenGLnames As New SortedList(Of String, String)
        Dim PYnames As New SortedList(Of String, String)
        Dim VBNames As New SortedList(Of String, String)
        Dim allButPython As New SortedList(Of String, String)
        Dim PYStreamNames As New SortedList(Of String, String)
        Dim LastEdits As New SortedList(Of String, String)

        Dim prefix As String = "../../../../"
        Dim CCInput = New FileInfo(prefix + "CPP_Code/CPP_AI_Generated.h")
        For i = 0 To 3
            If CCInput.Exists = False Then
                prefix = prefix.Substring(3)
                CCInput = New FileInfo(prefix + "CPP_Code/CPP_AI_Generated.h")
            Else
                Exit For
            End If
        Next
        If CCInput.Exists = False Then
            MsgBox("The UI_Generator code needs to be reviewed." + vbCrLf + "Either UI_Generator has moved or projects reference have." + vbCrLf +
                   CCInput.FullName + " was not found.")
            Exit Sub
        End If

        Dim HomeDir As New DirectoryInfo(CCInput.DirectoryName + "/../")
        ' New FileInfo(HomeDir.FullName + "CPP_Classes/CPP_Classes.cpp").FullName,
        Dim CS_CPPCLR_Inputs = {New FileInfo(HomeDir.FullName + "CS_Classes/CS_AI_Generated.cs").FullName,
                                New FileInfo(HomeDir.FullName + "CPP_Code/CPP_AI_Generated.h").FullName,
                                New FileInfo(HomeDir.FullName + "CS_Classes/Non_AI.cs").FullName}
        Dim VBcodeDir As New DirectoryInfo(HomeDir.FullName + "VB_classes/")
        Dim CPPInput As New DirectoryInfo(HomeDir.FullName + "CPP_classes/CPP_Classes.cpp")

        Dim OptionsFile = New FileInfo(VBcodeDir.FullName + "Options.vb")
        Dim vbOptions = New FileInfo(VBcodeDir.FullName + "/../VB_Classes/Options.vb")
        Dim includeOptions = New FileInfo(VBcodeDir.FullName + "/../CPP_Code/Options.h")
        Dim result As Integer
        If includeOptions.Exists Then
            result = DateTime.Compare(vbOptions.LastWriteTime, includeOptions.LastWriteTime)
        End If
        If result > 0 Then
            includeOptions.Delete()
            ConvertOptionsToCPP(OptionsFile)
        End If

        Dim indexTestFile = New FileInfo(HomeDir.FullName + "/Data/AlgorithmGroupNames.txt")
#If DEBUG Then
        If indexTestFile.Exists Then My.Computer.FileSystem.DeleteFile(indexTestFile.FullName)
#End If
        If indexTestFile.Exists And Not Debugger.IsAttached Then
            If checkDates(New DirectoryInfo(HomeDir.FullName + "/CS_Classes/"), indexTestFile) = False Then
                If checkDates(New DirectoryInfo(HomeDir.FullName + "/VB_Classes/"), indexTestFile) = False Then
                    If checkDates(New DirectoryInfo(HomeDir.FullName + "/CPP_Code/"), indexTestFile) = False Then
                        If checkDates(New DirectoryInfo(HomeDir.FullName + "/CPP_Classes/"), indexTestFile) = False Then
                            Console.WriteLine("The user interface is already up to date.")
                            Exit Sub ' nothing to trigger 
                        End If
                    End If
                End If
            End If
        End If
        Console.WriteLine("Starting work to generate the user interface.")

        Dim includeCC = File.ReadAllLines(CCInput.FullName)
        Dim ccLines As Integer
        For Each incline In includeCC
            incline = Trim(incline)
            If incline.StartsWith("//") Then Continue For
            If incline.Length = 0 Then Continue For
            ccLines += 1
        Next

        Dim includeCPP = File.ReadAllLines(CPPInput.FullName)
        Dim cppLines As Integer
        For Each algline In includeCPP
            algline = Trim(algline)
            If algline.StartsWith("//") Then Continue For
            If algline = "{" Or algline = "}" Then Continue For
            If algline.Length = 0 Then Continue For
            cppLines += 1
        Next

        Dim csLines As Integer
        For Each csFile In CS_CPPCLR_Inputs
            Dim CSAlgorithms = File.ReadAllLines(csFile)
            For Each algline In CSAlgorithms
                algline = Trim(algline)
                If algline = "{" Or algline = "}" Then Continue For
                If algline.StartsWith("//") Then Continue For
                If algline.Length = 0 Then Continue For
                csLines += 1
            Next
        Next

        ' first read all the CPP_Code functions that are present in the project
        Dim functionInput As New FileInfo(HomeDir.FullName + "/CPP_Code/CPP_Enum.h")
        Dim srFunctions = New StreamReader(functionInput.FullName)
        Dim ccFunctionNames As New SortedList(Of String, String)
        Dim unsortedFunctions As New List(Of String)
        While srFunctions.EndOfStream = False
            Dim cppline = srFunctions.ReadLine()
            If cppline.Contains("enum functions") Then
                While 1
                    cppline = Trim(srFunctions.ReadLine())
                    If cppline = "{" Then Continue While
                    If cppline = "};" Then Exit While
                    Dim split = cppline.Split(",")
                    If split(0).Contains("MAX_FUNCTION = ") Then Continue While
                    ccFunctionNames.Add(split(0).Substring(0).Trim(), split(0).Trim())
                    unsortedFunctions.Add(split(0).Substring(0).Trim())
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
        Dim CodeLineCount As Integer = ccLines + cppLines + csLines ' now adding in the C++ and C# lines...
        Dim sortedNames As New SortedList(Of String, Integer)
        For Each fileName In fileNames
            If fileName.EndsWith(".py") And fileName.Contains("__init") = False Then
                Dim fileinfo As New FileInfo(fileName)
                If sortedNames.Keys.Contains(fileinfo.Name) = False Then sortedNames.Add(fileinfo.Name, sortedNames.Count)
                fileName = fileinfo.FullName
            Else
                If fileName.EndsWith("VB_Parent.vb") Then Continue For
                If fileName.EndsWith("AlgorithmList.cs") Then Continue For
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
        Next

        Dim csSortedNames As New SortedList(Of String, Integer)
        Dim cppSortedNames As New SortedList(Of String, Integer)
        Dim csFileNames As New List(Of String)
        Dim csAdds As New List(Of String)
        Dim cppAdds As New List(Of String)
        ' we only want python files that are included in the Python_Classes Project.  Other Python files may be support modules or just experiments.
        Dim csAppDir As New IO.DirectoryInfo(HomeDir.FullName + "/CS_Classes/")
        Dim csFiles() As String = Directory.GetFiles(csAppDir.FullName, "*.cs", SearchOption.AllDirectories)
        For Each csFile As String In csFiles
            csFileNames.Add(csFile)
        Next
        For Each fileName In CS_CPPCLR_Inputs
            If fileName.EndsWith("CS_Parent.cs") = False Then
                Dim csName As String = ""
                Dim cppName As String = ""
                Dim nextFile As New System.IO.StreamReader(fileName)
                While nextFile.Peek() <> -1
                    Dim line = Trim(nextFile.ReadLine())
                    If line Is Nothing Then Continue While
                    If line.Length > 1 Then
                        If line.Substring(0, 2) <> "//" Then
                            CodeLineCount += 1
                            If LCase(line).StartsWith("public class ") Or line.Contains("public CPP_Parent") Then
                                Dim split As String() = Regex.Split(line, "\W+")
                                If line.EndsWith(" : VB_Parent") Then
                                    csName = split(2)
                                    If csAdds.Contains(fileName) = False Then
                                        csAdds.Add(fileName)
                                        fileNames.Add(fileName)
                                    End If
                                End If
                                If line.EndsWith("public CPP_Parent") Then
                                    If line.Contains(" ref ") Then cppName = split(3) Else cppName = split(2)
                                    If cppAdds.Contains(fileName) = False Then
                                        cppAdds.Add(fileName)
                                        fileNames.Add(fileName)
                                    End If
                                End If
                                If line.Contains("_CC ") Then
                                    cppName = split(1)
                                    If cppAdds.Contains(fileName) = False Then
                                        cppAdds.Add(fileName)
                                        fileNames.Add(fileName)
                                    End If
                                End If
                            End If
                            If LCase(line).StartsWith("public ") And csSortedNames.ContainsKey(csName) = False And
                                csName <> "" Then
                                csSortedNames.Add(csName, csSortedNames.Count)
                            End If
                            If LCase(line).StartsWith("public ") And cppSortedNames.ContainsKey(cppName) = False And
                                cppName <> "" Then
                                cppSortedNames.Add(cppName, cppSortedNames.Count)
                            End If
                            If cppName.EndsWith("_CC") And cppSortedNames.ContainsKey(cppName) = False And
                                cppName <> "" Then
                                cppSortedNames.Add(cppName, cppSortedNames.Count)
                            End If
                        End If
                    End If
                End While
            End If
        Next

        For Each name In cppSortedNames.Keys
            sortedNames.Add(name, sortedNames.Count)
        Next

        For Each name In csSortedNames.Keys
            sortedNames.Add(name, sortedNames.Count)
        Next

        Dim cleanNames As New List(Of String)
        Dim lastName As String = ""
        For i = 0 To sortedNames.Count - 1
            Dim nextName = sortedNames.ElementAt(i).Key
            If nextName <> lastName + ".py" Then cleanNames.Add(nextName)
            lastName = nextName
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
        For Each csName In csSortedNames.Keys
            sw.WriteLine(vbTab + vbTab + vbTab + "if (algorithmName == """ + csName + """) return new " + csName + "();")
        Next
        sw.WriteLine(vbTab + vbTab + vbTab + "return new AddWeighted_Basics_CS();")
        sw.WriteLine(vbTab + vbTab + "}")
        sw.WriteLine(vbTab + "}")
        sw.WriteLine("}")
        sw.Close()





        ' C++ output
        Dim CPPlistInfo As New FileInfo(HomeDir.FullName + "Main_UI\AlgorithmList.vb")
        sw = New StreamWriter(CPPlistInfo.FullName)
        sw.WriteLine("' this file is automatically generated in a pre-build step.  Any manual modifications will be lost.")
        ' sw.WriteLine("Imports CPP_Classes")
        sw.WriteLine("Imports CS_Classes")
        sw.WriteLine("Imports VB_Classes")

        sw.WriteLine("Public Class algorithmList")
        sw.WriteLine("Public Enum ccFunctionNames")
        For i = 0 To unsortedFunctions.Count - 1
            sw.WriteLine(unsortedFunctions(i))
        Next
        sw.WriteLine("End Enum")

        sw.WriteLine(vbTab + "Public Function createAlgorithm(algorithmName as string) as Object")
        sw.WriteLine(vbTab + "If algorithmName.endsWith("".py"") then return new Python_Run()")
        'For Each cppName In cppSortedNames.Key
        '    sw.WriteLine(vbTab + vbTab + "if algorithmName = """ + cppName + """ Then Return New " + cppName)
        'Next
        For Each nextName In cleanNames
            If nextName.StartsWith("CPP_Basics") Then Continue For
            If nextName.EndsWith("_CC") Then
                sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new CPP_Basics(ccFunctionNames._" + nextName + ")")
            Else
                If nextName.EndsWith(".py") = False Then
                    sw.WriteLine(vbTab + "If algorithmName = """ + nextName + """ Then return new " + nextName)
                End If
            End If
        Next
        sw.WriteLine(vbTab + vbTab + "Return Nothing")
        sw.WriteLine(vbTab + "End Function")
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
            sw.WriteLine(cleanNames(i))
            If cleanNames(i).EndsWith(".py") Then
                PYnames.Add(cleanNames(i), cleanNames(i))
                If cleanNames(i).EndsWith("_PS.py") Then PYStreamNames.Add(cleanNames(i), cleanNames(i))
            Else
                If cleanNames(i).EndsWith("_CS") = False And cleanNames(i).EndsWith("_CPP") = False Then
                    VBNames.Add(cleanNames(i), cleanNames(i))
                End If
                apiList.Add(cleanNames(i))
                apiListLCase.Add(LCase(cleanNames(i)))
                allButPython.Add(cleanNames(i), cleanNames(i))
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
                        Dim split = codeline.Split(" ")
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
        'Dim testkeys = New List(Of String)
        'For Each nm In CSnames.Keys
        '    testkeys.Add(nm.Substring(0, nm.Length - 3))
        'Next
        'For Each nm In VBNames.Keys
        '    If testkeys.Contains(nm) = False And nm.StartsWith("options_") = False Then
        '        If nm.EndsWith("_CPP_VB") = False And nm.StartsWith("Options_") = False Then
        '            Console.WriteLine("missing from c# code: " + nm)
        '            count += 1
        '        End If
        '    End If
        'Next

        sw = New StreamWriter(HomeDir.FullName + "Data/AlgorithmGroupNames.txt")
        Dim allCount = allButPython.Count + PYnames.Count
        sw.WriteLine("<All (" + CStr(allCount) + ")>")

        sw.Write("<All but Python (" + CStr(allButPython.Count) + ")>")
        For i = 0 To allButPython.Count - 1
            Dim nextName = allButPython.ElementAt(i).Key
            If nextName = "CPP_Basics" Then Continue For
            sw.Write("," + nextName)
        Next
        sw.WriteLine()

        sw.Write("<All C# (" + CStr(CSnames.Count) + ")>")
        For i = 0 To CSnames.Count - 1
            sw.Write("," + CSnames.ElementAt(i).Key)
        Next
        sw.WriteLine()


        Dim ccNames As New List(Of String)
        For Each nm In allButPython.Keys
            If nm.Contains("_CPP_") Then ccNames.Add(nm)
            If nm.EndsWith("_CPP") Then ccNames.Add(nm)
            If nm.EndsWith("_CC") Then ccNames.Add(nm)
        Next

        sw.Write("<All C++ (" + CStr(ccNames.Count) + ")>")
        For i = 0 To ccNames.Count - 1
            sw.Write("," + ccNames(i))
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

        Dim rankList As New FileInfo(HomeDir.FullName + "Data/RankList.txt")
        Dim rankingInput As New FileInfo(HomeDir.FullName + "Data/AlgorithmGroupNames.txt")
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
            Dim result As Integer = DateTime.Compare(fileInfo.LastWriteTime, algorithmGroupNames.LastWriteTime)
            If result > 0 Then Return True
        Next
        Return False
    End Function
    Private Sub ConvertOptionsToCPP(inputfile As FileInfo)
        Dim index As Integer = 0
        Dim output As New List(Of String)
        Dim phase1 As New List(Of String)
        Dim phase2 As New List(Of String)
        Dim phase3 As New List(Of String)
        Dim lines = File.ReadAllLines(inputfile.FullName)

        index = 0
        While index < lines.Count
            Dim line = Trim(lines(index))
            index += 1
            If line.StartsWith("Public Class Options_") Then
                For i = index To lines.Count - 1
                    line = line.Replace(" = True", " = true")
                    line = line.Replace(" = False", " = false")
                    line = line.Replace("(Of String)", "(Of string)")
                    line = line.Replace("Nothing", "null")

                    If line.Contains(" As New ") Then
                        If line.Contains(" As New List(Of ") = False Then line = ""
                    End If

                    phase1.Add(line)
                    line = Trim(lines(index))
                    index += 1
                    If line.Contains("Public Sub New") Then
                        phase1.Add(line)
                        Exit For
                    End If
                Next
            End If
        End While

        index = 0
        While index < phase1.Count
            Dim line = Trim(phase1(index))
            index += 1
            If line.Contains("{") Then
                Dim arrayLine = ""
                For i = index To phase1.Count - 1
                    If line.Contains("}") Then
                        arrayLine += line
                        phase2.Add(arrayLine)
                        Exit For
                    Else
                        arrayLine += line
                    End If
                    line = Trim(phase1(index))
                    index += 1
                Next
            Else
                phase2.Add(line)
            End If
        End While

        index = 0
        While index < phase2.Count
            Dim line = Trim(phase2(index))
            index += 1

            If line.Contains("TrackBar") Then Continue While
            If line.Contains("CheckBox") Then Continue While
            If line.Contains("RadioButton") Then Continue While
            If line.Contains("FileInfo") Then Continue While
            If line.Contains("fileName") Then Continue While
            If line.Contains("fileNames ") Then Continue While

            line = line.Replace("cvb.", "cv::")
            If line.StartsWith("Public Class Options_") Or line.Contains("Public Sub New") Then
                phase3.Add(line)
                Continue While
            End If

            If line.Contains(" As cv.Mat") Then Continue While

            phase3.Add(line)
        End While

        index = 0
        output.Add("// This file is automatically generated.  Don't waste your time altering it.")
        output.Add("// This file provides initialized variables for all the options to the C++ code.")
        output.Add("// It is statically defined because C++ options must be provided in C++ code")
        output.Add("// which is more work to be done when moving this code to another application.")
        output.Add("#include <string.h>")
        output.Add("using namespace cv;")
        output.Add("using namespace std;")
        While index < phase3.Count
            Dim line = Trim(phase3(index))
            index += 1
            If line.StartsWith("Public Class Options_") Then
                Dim splitLine = line.Split(" ")
                output.Add("class " + splitLine(2) + " {")
                output.Add("public:")
                For i = index To phase3.Count - 1
                    line = Trim(phase3(i))
                    index += 1
                    If line = "" Then Continue For
                    If line.StartsWith("Public Sub New") Then
                        output.Add(vbTab + "void RunOpt() {}")
                        output.Add(vbTab + splitLine(2) + "() {")
                        output.Add(vbTab + "}")
                        output.Add("};")
                        Exit For
                    Else

                        Dim split = line.Split(" ")
                        If split(3) = "Integer" Then split(3) = "int"
                        If split(3) = "Boolean" Then split(3) = "bool"
                        If split(3) = "Single" Then split(3) = "float"
                        If split(3) = "Double" Then split(3) = "double"
                        If split(3) = "String" Then split(3) = "string"
                        split(1) = split(1).Replace("()", "[]")

                        If line.Contains(" As New List(Of") Then
                            Dim listtype = split(5).Substring(0, Len(split(5)) - 1)
                            output.Add("vector<" + listtype + "> " + split(1) + ";")
                        Else
                            If line.Contains(" = ") Then
                                If line.Contains("reduceXYZ") Then
                                    output.Add(vbTab + "bool reduceXYZ[3] = {true, true, true};")
                                Else
                                    Dim splitEqual = line.Split("=")
                                    splitEqual(1) = splitEqual(1).Replace(" New ", " ")
                                    output.Add(vbTab + split(3) + " " + split(1) + " = " + splitEqual(1) + ";")
                                End If
                            Else
                                If line.Contains("buffer(9)") Then output.Add(vbTab + "int buffer[9];")
                                If line.Contains("inputPoints()") Then output.Add(vbTab + "Point2f inputPoints[];")
                                If line.Contains("radioChoices()") Then output.Add(vbTab + "Vec3i radioChoices[];")
                                If line.Contains("splits(4)") Then output.Add(vbTab + "int splits[5];")
                                If line.Contains("vals(4)") Then output.Add(vbTab + "int vals[5];")
                            End If
                        End If
                    End If
                Next
            End If
        End While

        ' special case language dependent changes.
        For i = 0 To output.Count - 1
            Dim nextLine = output(i)
            output(i) = ""
            nextLine = nextLine.Replace("ContourApproximationModes.", "ContourApproximationModes::")
            nextLine = nextLine.Replace("ApproxTC89KCOS", "CHAIN_APPROX_TC89_KCOS")
            nextLine = nextLine.Replace("ApproxNone", "CHAIN_APPROX_NONE")
            nextLine = nextLine.Replace("ApproxSimple", "CHAIN_APPROX_SIMPLE")
            nextLine = nextLine.Replace("ApproxTC89KCOS", "CHAIN_APPROX_TC89_KCOS")
            nextLine = nextLine.Replace("ApproxTC89L1", "CHAIN_APPROX_TC89_L1")
            nextLine = nextLine.Replace("RetrievalModes.", "RetrievalModes::")
            nextLine = nextLine.Replace("RetrievalModes::External", "cv::RetrievalModes::RETR_EXTERNAL")
            nextLine = nextLine.Replace("ImwriteFlags.JpegProgressive", "ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE")
            nextLine = nextLine.Replace("ImwriteFlags::IMWRITE_JPEG_PROGRESSIVE ", "int ")

            nextLine = nextLine.Replace("ShapeMatchModes.I1", " ShapeMatchModes::CONTOURS_MATCH_I1")
            nextLine = nextLine.Replace("InterpolationFlags.Nearest", "InterpolationFlags::INTER_NEAREST")
            nextLine = nextLine.Replace("ML.SVM.Types.CSvc", "ml::SVM::C_SVC")
            nextLine = nextLine.Replace("ML.SVM.KernelTypes ", "int ")
            nextLine = nextLine.Replace("ML.SVM.KernelTypes.Poly", "ml::SVM::KernelTypes::POLY")
            nextLine = nextLine.Replace("DctFlags ", "int ")
            nextLine = nextLine.Replace("DctFlags;", "cv::DCT_INVERSE | cv::DCT_ROWS;")
            nextLine = nextLine.Replace("ColorConversionCodes.BGR2GRAY", "ColorConversionCodes::COLOR_BGR2GRAY")
            nextLine = nextLine.Replace("OpticalFlowFlags.FarnebackGaussian;", "cv::OPTFLOW_FARNEBACK_GAUSSIAN;")
            nextLine = nextLine.Replace("OpticalFlowFlags OpticalFlowFlag", "int OpticalFlowFlag")
            If nextLine.Contains("Quaternion") Then Continue For

            nextLine = nextLine.Replace("HomographyMethods.None ", "int ")
            nextLine = nextLine.Replace("HomographyMethods.None;", "LMEDS;")
            nextLine = nextLine.Replace("Math.PI", "CV_PI")
            nextLine = nextLine.Replace("SortFlags.EveryColumn", "cv::SortFlags::SORT_EVERY_COLUMN")
            nextLine = nextLine.Replace("SortFlags.Ascending", "cv::SortFlags::SORT_ASCENDING")
            nextLine = nextLine.Replace("SortFlags sortOption ", "int sortOption")
            nextLine = nextLine.Replace("DistanceTypes ", "int ")
            nextLine = nextLine.Replace("DistanceTypes.L1", "DistanceTypes::DIST_L1")
            nextLine = nextLine.Replace("HistCompMethods.Correl", "cv::HistCompMethods::HISTCMP_CORREL")
            nextLine = nextLine.Replace("EMTypes.CovMatDefault ", "int ")
            If nextLine.Contains("CovMatDefault") Then Continue For

            nextLine = nextLine.Replace("KMeansFlags ", "int ")
            nextLine = nextLine.Replace("KMeansFlags.RandomCenters", "KmeansFlags::KMEANS_RANDOM_CENTERS")
            nextLine = nextLine.Replace(" Or ", " | ")
            nextLine = nextLine.Replace("FloodFillFlags ", "int ")
            nextLine = nextLine.Replace("MorphShapes.Cross", "cv::MorphShapes::MORPH_CROSS")
            nextLine = nextLine.Replace("TemplateMatchModes.CCoeffNormed", "cv::TemplateMatchModes::TM_CCOEFF_NORMED")
            nextLine = nextLine.Replace("TemplateMatchModes ", "int ")
            nextLine = nextLine.Replace("ThresholdTypes.Binary", "cv::ThresholdTypes::THRESH_BINARY")
            nextLine = nextLine.Replace("AdaptiveThresholdTypes.GaussianC", "cv::AdaptiveThresholdTypes::ADAPTIVE_THRESH_GAUSSIAN_C")
            nextLine = nextLine.Replace("DftFlags ", "int ")
            nextLine = nextLine.Replace("DftFlags.ComplexOutput", "DftFlags::DFT_COMPLEX_OUTPUT")
            nextLine = nextLine.Replace("SeamlessCloneMethods ", "int ")
            nextLine = nextLine.Replace("SeamlessCloneMethods.MixedClone", "cv::MIXED_CLONE")
            nextLine = nextLine.Replace("DecompTypes ", "int ")
            nextLine = nextLine.Replace("DecompTypes.Cholesky", "DecompTypes::DECOMP_CHOLESKY")
            nextLine = nextLine.Replace("SimpleBlobDetector.Params ", "SimpleBlobDetector::Params ")


            nextLine = nextLine.Replace("FloodFillFlags.FixedRange", "FloodFillFlags::FLOODFILL_FIXED_RANGE")

            If nextLine.Contains("SimpleBlobDetector.Params") Then Continue For

            nextLine = nextLine.Replace("cv::int ", "int ")
            nextLine = nextLine.Replace("cv::cv::", "cv::")
            output(i) = nextLine
        Next

        Dim outFile = New FileInfo(inputfile.Directory.FullName + "/../CPP_Code/Options.h")
        File.WriteAllLines(outFile.FullName, output)
    End Sub
End Module
