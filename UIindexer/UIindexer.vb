Imports System.IO
Imports System.Text.RegularExpressions
Module IndexMain
    Dim CSnames As New SortedList(Of String, String)
    Dim OpenGLnames As New SortedList(Of String, String)
    Dim PYnames As New SortedList(Of String, String)
    Dim multiThreaded As New SortedList(Of String, String)
    Dim nonPYnames As New SortedList(Of String, String)
    Dim onlyCPP As New SortedList(Of String, String)
    Dim PYStreamNames As New SortedList(Of String, String)
    Dim Painterly As New SortedList(Of String, String)
    Dim MatchShapes As New SortedList(Of String, String)
    Dim LastEdits As New SortedList(Of String, String)
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
    Sub Main()
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../vb_classes/")
        Dim dir As New DirectoryInfo(VBcodeDir.FullName)
        Dim fileList As List(Of FileInfo) = dir.GetFiles().ToList()
        fileList.Sort(AddressOf SortByDate)
        Dim filesByDate As New List(Of String)
        For Each entry In fileList
            If entry.Name.EndsWith(".vb") Then filesByDate.Add(entry.Name)
        Next

        Dim apiList As New List(Of String)
        Dim apiListLCase As New List(Of String)
        Dim classNames As New List(Of String) ' the list of all the classnames - including python names 
        Dim line As String
        Dim ExecDir As New DirectoryInfo(My.Application.Info.DirectoryPath)
        ChDir(ExecDir.FullName)
        Dim directoryInfo As New DirectoryInfo(CurDir() + "/../../vb_classes")
        ' Process the list of files found in the directory. 
        Dim sr = New System.IO.StreamReader(directoryInfo.FullName + "\..\Data\FileNames.txt")
        Dim codeFileNames As New List(Of String)
        While sr.EndOfStream = False
            codeFileNames.Add(sr.ReadLine)
        End While

        ' read the list of OpenCV API's we will be looking for
        Dim srAPI = New System.IO.StreamReader(directoryInfo.FullName + "\..\Data\OpenCVapi.txt")
        While srAPI.EndOfStream = False
            line = srAPI.ReadLine()
            If line <> "" Then
                apiListLCase.Add(LCase(line) + "(") ' it needs the parenthesis to make sure it is a function.
                apiList.Add(line + "(") ' it needs the parenthesis to make sure it is a function.
            End If
        End While
        srAPI.Close()

        Dim apiOCVB = New System.IO.StreamReader(directoryInfo.FullName + "\..\Data\AlgorithmList.txt")
        line = apiOCVB.ReadLine() ' toss the codeline count...
        While apiOCVB.EndOfStream = False
            line = apiOCVB.ReadLine()
            If line.EndsWith(".py") Then
                PYnames.Add(line, line)
                If line.EndsWith("_PS.py") Then PYStreamNames.Add(line, line)
            Else
                If line <> "" Then
                    If line.Contains("Python_Stream") = False And line.Contains("Python") = False Then
                        nonPYnames.Add(line, line)
                        apiList.Add(line)
                        apiListLCase.Add(LCase(line))
                        If line.StartsWith("CPP_") Or line.EndsWith("_CPP") Then onlyCPP.Add(line, line)
                    End If
                End If
            End If
        End While
        apiOCVB.Close()

        Dim tokens(apiList.Count - 1) As String
        For Each fileName In codeFileNames
            Dim info = New FileInfo(fileName)

            Dim dateIndex = filesByDate.IndexOf(info.Name)
            Dim recentEdit As Boolean = False
            If dateIndex > filesByDate.Count - 20 Then recentEdit = True
            dateIndex = filesByDate.Count - dateIndex

            Dim nextFile As New System.IO.StreamReader(info.FullName)
            Dim classname As String = ""
            If info.Name.EndsWith(".py") Then classname = info.Name ' python file names are the class name - they don't have multiple classnames per file

            While nextFile.Peek() <> -1
                line = Trim(nextFile.ReadLine())
                Dim lcaseLine = " " + LCase(line)
                If line.Contains("Parallel.For") Then
                    If multiThreaded.ContainsKey(classname) = False Then multiThreaded.Add(classname, classname)
                End If
                If lcaseLine.Contains("painterly") And Painterly.ContainsKey(classname) = False Then Painterly.Add(classname, classname)
                If lcaseLine.Contains("matchshapes: ") And MatchShapes.ContainsKey(classname) = False Then MatchShapes.Add(classname, classname)
                If line = "" Or Trim(line).StartsWith("'") Or Trim(line).StartsWith("#") Then Continue While
                If LCase(line).StartsWith("public class") And LCase(line).EndsWith("inherits vb_algorithm") Then
                    Dim split As String() = Regex.Split(line, "\W+")
                    classname = split(2) ' public class <classname>
                    If classname.StartsWith("Python_") Then PYnames.Add(classname, classname)
                    If classname.EndsWith("_PS.py") Then PYStreamNames.Add(classname, classname)
                    If classname.StartsWith("OpenGL") Then OpenGLnames.Add(classname, classname)
                    If classname.StartsWith("OpenCVGL") Then OpenGLnames.Add(classname, classname)
                    If recentEdit And classname.StartsWith("Options_") = False Then
                        LastEdits.Add(Format(dateIndex, "0000") + " " + classname, classname)
                        recentEdit = False ' just get the first algorithm in the module to suggest where recent changes occurred.
                    End If
                    Continue While
                End If
                If classname <> "" Then
                    If line.Contains("CS_Classes.") And CSnames.ContainsKey(classname) = False Then CSnames.Add(classname, classname)
                    If line.Contains("New OpenGL") And classname.StartsWith("OpenGL") = False Then OpenGLnames.Add(classname, classname)
                    For i = 0 To apiList.Count - 1
                        Dim index = InStr(lcaseLine, apiListLCase(i))
                        If index > 0 Then
                            If isAlpha(lcaseLine.Substring(index - 2, 1)) = False Then
                                If tokens(i) Is Nothing Then
                                    tokens(i) = classname
                                Else
                                    If tokens(i).Contains(classname) = False Then tokens(i) += "," + classname
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

        Dim sortedNames As New SortedList(Of String, String)
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
                sortedNames.Add(apiList(i), finalEntry)
            End If
        Next

        Dim sw As New StreamWriter(directoryInfo.FullName + "/../Data/AlgorithmMapToOpenCV.txt")
        sw.WriteLine("<All>")

        sw.Write("<All but Python>")
        For i = 0 To nonPYnames.Count - 1
            sw.Write("," + nonPYnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.WriteLine("<All using recorded data>")

        sw.Write("<C++>")
        For i = 0 To onlyCPP.Count - 1
            sw.Write("," + onlyCPP.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<C#>")
        For i = 0 To CSnames.Count - 1
            sw.Write("," + CSnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<Changed Recently>")
        For i = 0 To LastEdits.Count - 1
            sw.Write("," + LastEdits.ElementAt(i).Value)
        Next
        sw.WriteLine()

        sw.Write("<MatchShapes>")
        For i = 0 To MatchShapes.Count - 1
            sw.Write("," + MatchShapes.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<OpenGL>")
        For i = 0 To OpenGLnames.Count - 1
            sw.Write("," + OpenGLnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<Painterly>")
        For i = 0 To Painterly.Count - 1
            sw.Write("," + Painterly.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<Python>")
        For i = 0 To PYnames.Count - 1
            sw.Write("," + PYnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<PyStream>")
        For i = 0 To PYStreamNames.Count - 1
            sw.Write("," + PYStreamNames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        For i = 0 To sortedNames.Count - 1
            Dim token = sortedNames.ElementAt(i)
            sw.WriteLine(token.Key + "," + sortedNames.ElementAt(i).Value)
        Next
        sw.Close()
    End Sub
End Module