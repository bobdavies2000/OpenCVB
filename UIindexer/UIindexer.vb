Imports System.IO
Imports System.Text.RegularExpressions
Module IndexMain
    Dim CPPnames As New SortedList(Of String, String)
    Dim MTnames As New SortedList(Of String, String)
    Dim CSnames As New SortedList(Of String, String)
    Dim OpenGLnames As New SortedList(Of String, String)
    Dim rankings(5 - 1) As SortedList(Of String, String)
    Dim numpy As New SortedList(Of String, String)
    Dim PYnames As New SortedList(Of String, String)
    Dim nonPYnames As New SortedList(Of String, String)
    Dim PYStreamNames As New SortedList(Of String, String)
    Dim Painterly As New SortedList(Of String, String)
    Dim Basics As New SortedList(Of String, String)
    Dim MoreWork As New SortedList(Of String, String)
    Dim Trackers As New SortedList(Of String, String)
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
    Sub Main()
        For i = 0 To rankings.Length - 1
            rankings(i) = New SortedList(Of String, String)
        Next
        Dim apiList As New List(Of String)
        Dim apiListLCase As New List(Of String)
        Dim classNames As New List(Of String) ' the list of all the classnames - including python names 
        Dim line As String
        Dim ExecDir As New DirectoryInfo(My.Application.Info.DirectoryPath)
        ChDir(ExecDir.FullName)
        Dim directoryInfo As New DirectoryInfo(CurDir() + "/../../vb_classes")
        Dim fileEntries As String() = Directory.GetFiles(directoryInfo.FullName)
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

        ' add any custom keywords here.  These are OpenCVB terms not OpenCV API's (so no right parenthesis) - case sensitive and sorted!
        Dim ocvbKeywords() As String = {"mouseClickPoint", "mouseClickFlag", "mousePicTag", "mousePoint", "testAllRunning"}
        For i = 0 To ocvbKeywords.Length - 1
            apiListLCase.Add(LCase(ocvbKeywords(i))) ' no "(" in the lower case edition - these are not function calls.
            apiList.Add(ocvbKeywords(i) + "(")
        Next

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
                        If line.EndsWith("_Basics") Then Basics.Add(line, line)
                        nonPYnames.Add(line, line)
                        apiList.Add(line)
                        apiListLCase.Add(LCase(line))
                    End If
                End If
            End If
        End While
        apiOCVB.Close()

        Dim tokens(apiList.Count - 1) As String
        For Each fileName In codeFileNames
            Dim info = New FileInfo(fileName)
            Dim nextFile As New System.IO.StreamReader(info.FullName)
            Dim classname As String = ""
            If info.Name.EndsWith(".py") Then classname = info.Name ' python file names are the class name - they don't have multiple classnames per file

            While nextFile.Peek() <> -1
                line = Trim(nextFile.ReadLine())
                Dim lcaseLine = " " + LCase(line)
                If lcaseLine.Contains("painterly") And Painterly.ContainsKey(classname) = False Then Painterly.Add(classname, classname)
                If line.Contains("task.rank = ") Then
                    Dim rankVal = CInt(line.Substring(Len(line) - 1, 1))
                    If rankings(rankVal - 1).ContainsKey(classname) = False Then rankings(rankVal - 1).Add(classname, classname)
                End If

                If line = "" Or Trim(line).StartsWith("'") Or Trim(line).StartsWith("#") Then Continue While

                If lcaseLine.Contains("needs more work") And MoreWork.ContainsKey(classname) = False Then MoreWork.Add(classname, classname)
                If lcaseLine.Contains("tracker algorithm") And Trackers.ContainsKey(classname) = False Then Trackers.Add(classname, classname)
                If (lcaseLine.Contains("np.") Or LCase(classname).Contains("numpy")) And numpy.ContainsKey(classname) = False Then numpy.Add(classname, classname)
                If LCase(line).StartsWith("public class") Then
                    Dim split As String() = Regex.Split(line, "\W+")
                    ' next line must be "Inherits VBparent"
                    Dim line2 = Trim(nextFile.ReadLine())
                    If line2 = "Inherits VBparent" Then
                        classname = split(2) ' public class <classname>
                    End If
                    If classname.StartsWith("Python_") Then PYnames.Add(classname, classname)
                    If classname.EndsWith("_PS.py") Then PYStreamNames.Add(classname, classname)
                    If classname.EndsWith("_MT") Then MTnames.Add(classname, classname)
                    If classname.EndsWith("_CPP") Then CPPnames.Add(classname, classname)
                    If classname.StartsWith("OpenGL") Then OpenGLnames.Add(classname, classname)
                    If classname.StartsWith("OpenCVGL") Then OpenGLnames.Add(classname, classname)
                    Continue While
                End If
                If classname <> "" Then
                    If line.Contains("CS_Classes.") And CSnames.ContainsKey(classname) = False Then CSnames.Add(classname, classname)
                    If line.Contains("New OpenGL") And classname.StartsWith("OpenGL") = False And classname.StartsWith("OpenCVGL") = False Then OpenGLnames.Add(classname, classname)
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

        sw.Write("<Basics>")
        For i = 0 To Basics.Count - 1
            sw.Write("," + Basics.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<C++>")
        For i = 0 To CPPnames.Count - 1
            sw.Write("," + CPPnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<C#>")
        For i = 0 To CSnames.Count - 1
            sw.Write("," + CSnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        If MoreWork.Count > 0 Then
            sw.Write("<MoreWork>")
            For i = 0 To MoreWork.Count - 1
                sw.Write("," + MoreWork.ElementAt(i).Key)
            Next
            sw.WriteLine()
        End If

        sw.Write("<Multi-Threaded Algorithms>")
        For i = 0 To MTnames.Count - 1
            sw.Write("," + MTnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<NumPy>")
        For i = 0 To numpy.Count - 1
            sw.Write("," + numpy.ElementAt(i).Key)
        Next
        sw.WriteLine()

        sw.Write("<OpenGL>")
        For i = 0 To OpenGLnames.Count - 1
            sw.Write("," + OpenGLnames.ElementAt(i).Key)
        Next
        sw.WriteLine()

        For i = 0 To ocvbKeywords.Count - 1
            sw.Write("<OpenCVB - " + ocvbKeywords(i) + ">,")
            Dim j = sortedNames.IndexOfKey(ocvbKeywords(i))
            sw.WriteLine(sortedNames.ElementAt(j).Value)
        Next

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

        For i = 0 To rankings.Length - 1
            If rankings(i).Count > 0 Then
                sw.Write("<Rank " + CStr(i + 1) + ">")
                For j = 0 To rankings(i).Count - 1
                    If rankings(i).ElementAt(j).Key <> "" Then sw.Write("," + rankings(i).ElementAt(j).Key)
                Next
                sw.WriteLine()
            End If
        Next

        sw.Write("<Trackers>")
        For i = 0 To Trackers.Count - 1
            sw.Write("," + Trackers.ElementAt(i).Key)
        Next
        sw.WriteLine()

        For i = 0 To sortedNames.Count - 1
            Dim token = sortedNames.ElementAt(i)
            sw.WriteLine(token.Key + "," + sortedNames.ElementAt(i).Value)
        Next
        sw.Close()
    End Sub
End Module