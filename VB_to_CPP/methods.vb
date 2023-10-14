Imports System.Collections.Generic
Module methods
    Public algorithmVBName As String
    Public fIndexName As String
    Public CPPName As String
    Public cppLines As New List(Of String)
    Public vbKeywords As New List(Of String)
    Public vbModule As String = ""
    Public ocvKeywords As New List(Of String)
    Public ocvbKeywords As New List(Of String)
    Public sliderText As New SortedList(Of String, String)
    Public checkBoxText As New List(Of String)
    Public radioText As New List(Of String)
    Public Function replaceContains(line As String, tabs As String)
        If line.Contains(".Contains(") Then
            Dim split = line.Split(". ".ToCharArray)
            Dim tSplit = line.Split("()".ToCharArray)
            Dim containStr As String = ""
            For i = 0 To tSplit.Count - 1
                If tSplit(i).Contains("Contains") Then
                    containStr = tSplit(i + 1)
                    Exit For
                End If
            Next
            For i = 0 To split.Length - 1
                If split(i).Contains("Contains") Then
                    line = line.Replace(split(i - 1) + ".Contains(" + containStr + ")",
                           "count(" + split(i - 1) + ".begin(), " + split(i - 1) + ".end(), " + containStr + ")")
                    Return line
                End If
            Next
        End If
        Return line
    End Function
    Public Function findFunction(line As String, tabs As String) As String
        Dim split = line.Split(" ,()".ToCharArray)
        If split(1) <> "Function" Then Return line
        If split(split.Length - 3) <> "As" Then Return line
        line = tabs + split(split.Length - 2) + " " + split(2) + "("
        For i = 3 To split.Length - 1
            If split(i) = "As" And split(i - 1) <> "" Then
                line += split(i + 1) + " " + split(i - 1) + ", "
            End If
        Next
        line = line.Substring(0, line.Length - 2) + ") {"
        Return line
    End Function
    Public Function replaceErase(line As String, tabs As String)
        Dim trimLine = line.Trim()
        Dim offset = InStr(trimLine, ".erase")
        Dim vecName = trimLine.Substring(0, offset)
        line = line.Replace("(0)", "(" + vecName + "begin())")
        Return line
    End Function
    Public Function scalarAllReplace(line As String, tabs As String)
        If line.Contains("Scalar.All") = False Then Return line
        Dim split = line.Split("()".ToCharArray)
        For i = 0 To split.Length - 1
            If split(i).EndsWith("Scalar.All") Then
                line = line.Replace("Scalar.All(" + split(i + 1) + ")", "Scalar(" + split(i + 1) + ", " + split(i + 1) + ", " + split(i + 1) + ")")
            End If
        Next
        Return line
    End Function
    Public Function findSub(line As String, tabs As String) As String
        Dim split = line.Split(" ,()".ToCharArray)
        If split(1) <> "Sub" Then Return line
        Dim part2 = line.Substring(InStr(line, "(") - 1)
        line = tabs + "void " + split(2) + part2
        Return line
    End Function
    Public Function findSortedList(line As String, tabs As String) As String
        Dim split = line.Split(" ")
        line = tabs + "map<int, int> " + split(1) + ";"
        Return line
    End Function
    Public Function buildSwitch(index As Integer) As Integer
        Dim tabs = countTabs(cppLines(index))
        Dim i = index
        cppLines(i) = cppLines(i).Replace("Select Case ", "switch (")
        cppLines(i) = cppLines(i).Replace(";", ") " + vbCrLf + tabs + "{")
        For i = i + 1 To cppLines.Count - 1
            If cppLines(i).Trim.StartsWith("Case ") Then
                cppLines(i) = cppLines(i).Replace("Case ", "case ")
                cppLines(i) = cppLines(i) + ":"
                If i > index + 1 Then
                    tabs = tabs.Substring(0, tabs.Length - 1)
                    cppLines(i) = tabs + vbTab + "break;" + vbCrLf + tabs + "}" + vbCrLf + cppLines(i) + vbCrLf + tabs + "{"
                Else
                    cppLines(i) = cppLines(i) + vbCrLf + tabs + "{"
                End If
                tabs += vbTab
            ElseIf cppLines(i).Trim.StartsWith("End Select") Then
                tabs = tabs.Substring(0, tabs.Length - 1)
                cppLines(i) = tabs + vbTab + "break;" + vbCrLf + tabs + "}" + vbCrLf + tabs + "}"
                Exit For
            End If
        Next
        Return i
    End Function
    Public Function markForLoop(str As String) As String
        Dim line = str.Trim
        If line.StartsWith("For ") = False Then Return str
        Dim tabs = countTabs(str)
        Dim split = line.Split(" ")
        str = str.Trim.Replace("For ", "// Review this For Loop >>>> ")
        Return tabs + str + vbCrLf + tabs + "for (auto " + split(1) + " = 0; " + split(1) + " < " + split(5) + "; " + split(1) + "++) {"
    End Function
    Public Function validateDST(str As String) As String
        For i = 0 To 4
            Dim dst = Choose(i + 1, "dst0", "dst1", "dst2", "dst3", "dst")
            If str.Contains(dst + ".width") Or str.Contains(dst + ".height") Then
                str = str.Replace(dst + ".width", dst + ".cols")
                str = str.Replace(dst + ".height", dst + ".rows")
            End If
        Next
        Return str
    End Function
    Public Function opencvAPIs(str As String) As String
        For i = 0 To ocvbKeywords.Count - 1
            If str.Contains(ocvbKeywords(i) + "(") Then
                str = str.Replace(ocvbKeywords(i) + "(", ocvKeywords(i) + "(")
            End If
        Next
        Return str
    End Function
    Public Function validateNorms(str As String) As String
        str = str.Replace("cv.NormTypes.MinMax", "NORM_MINMAX")
        str = str.Replace("cv.NormTypes.INF", "NORM_INF")
        str = str.Replace("cv.NormTypes.L1", "NORM_L1")
        str = str.Replace("cv.NormTypes.L2", "NORM_L2")
        Return str
    End Function
    Public Function countTabs(Str As String) As String
        Dim tabCount As Integer
        For i = 0 To Str.Length - 1
            If Str.Substring(i, 1) = vbTab Then tabCount += 1 Else Exit For ' just the tabs at the start of the line.
        Next
        Return StrDup(tabCount, vbTab)
    End Function
    Public Function findDefaultValue(sText As String) As String
        For Each keyval In sliderText
            If keyval.Key.Contains(sText) Then
                Return keyval.Value
            End If
        Next
        Return ""
    End Function
    Private Function removeCVdot(line As String) As String
        If line.Contains("cv.") Then line = line.Replace("cv.", "")
        If line.Contains("Cv2.") Then line = line.Replace("Cv2.", "")
        Return line
    End Function
    Private Function changeNewCVMat(line As String) As String
        If line.Contains("New Mat(") Then
            line = line.Replace(", MatType.", ", ")
            line = line.Replace("New Mat(", "Mat(")
            line = line.Replace("Dim ", "Mat ")
        Else
            Return line
        End If
        Dim saveLine = line
        If line.Contains(", 0)") Then
            line = line.Replace(", 0)", ")")
            Dim split = line.Split(" ")
            line = split(0) + " " + split(1) + " " + split(2) + " " + split(3) + ";"
            If saveLine.EndsWith(", 0)") Then
                line += vbCrLf + vbTab + split(0) + ".setTo(0);"
            End If
        End If
        Return line
    End Function
    Private Function forLoopChange(line As String) As String
        If line.Contains("For i = 0") Or line.Contains("For j = 0") Or line.Contains("For y = 0") Or line.Contains("For x = 0") Then
            Dim split = line.Split
            line = "for (auto " + split(1) + " = 0; " + split(1) + " < " + split(5) + "; " + split(1) + "++) " + vbCrLf + vbTab + "{"
        End If
        Return line
    End Function
    Private Function changeStandAlone(line As String) As String
        If line.Contains("standalone") Then line = line.Replace("standalone", "cppFunction == " + fIndexName)
        Return line
    End Function
    Private Function changeEqualEqual(line As String) As String
        If line.StartsWith("If ") Then
            Dim tLine = line.ToLower()
            Dim index = InStr(tLine, " Then")
            Dim eIndex = InStr(line, " = ")
            If eIndex < index Then line = line.Replace(" = ", " == ")
        End If
        Return line
    End Function
    Public Function displayCPPLines() As String
        Dim cppLine As String = ""
        For Each line In cppLines
            If line <> "" Then
                If line.Trim() <> ";" Then cppLine += line + vbCrLf
            End If
        Next
        Return cppLine
    End Function
    Public Function prepCountNonZero(line As String) As String
        Dim split = line.Split(". ".ToCharArray)
        For i = 0 To split.Length - 1
            If split(i).Contains("CountNonZero") Then
                line = line.Replace(split(i - 1) + ".CountNonZero", "countNonZero(" + split(i - 1) + ")")
                Exit For
            End If
        Next
        Return line
    End Function
    Public Function prepInRange(original As String) As String
        Dim tabs = countTabs(original)
        Dim line = original.Trim
        Dim split = line.Split(". ".ToCharArray)
        Dim offset = InStr(line, "InRange(")
        Dim part2 = line.Substring(offset + 7)
        part2 = part2.Substring(0, Len(part2) - 2)
        If split(1) <> "=" And split(2) <> "=" Then Return original

        If split(3).Contains("InRange") And split(1) = "=" Then
            line = tabs + "inRange(" + split(2) + ", " + part2 + ", " + split(0) + ");"
        ElseIf split(4).Contains("InRange") And split(2) = "=" Then
            line = tabs + "Mat " + split(1) + ";" + vbCrLf + tabs + "inRange(" + split(3) + ", " + part2 + ", " + split(1) + ");"
        End If
        Return line
    End Function
    Public Function prepRectangle(line As String) As String
        Dim tabs = countTabs(line)
        Dim split = line.Split(".".ToCharArray)
        For i = 0 To split.Length - 1
            If split(i).Contains("Rectangle") Then
                line = line.Replace(split(i - 1).Trim + ".Rectangle(", "rectangle(" + split(i - 1).Trim + ", ")
                Exit For
            End If
        Next
        Return line
    End Function
    Private Function changeDimPublic(line As String, typeName As String) As String
        Dim tabs = countTabs(line)
        line = line.Trim()
        If line.StartsWith("Dim ") Then line = line.Replace("Dim ", typeName)
        If line.StartsWith("Public ") Then line = line.Replace("Public ", typeName)
        If line.StartsWith("Static ") Then line = line.Replace("Static ", typeName)
        Return tabs + line
    End Function
    Public Function dotGetSet(line As String) As String
        Dim original = line
        Dim split = line.Split("() ".ToCharArray)
        For i = 0 To split.Length - 1
            If split(i) = "Of" Then
                line = line.Replace(".Get(Of ", ".at<")
                line = line.Replace(".Set(Of ", ".at<")
                Dim part1 = line.Substring(0, InStr(line, "<"))
                Dim test = Len(split(i + 1)) + 1
                Dim part2 = line.Substring(part1.Length + test)
                line = part1 + split(i + 1) + ">" + part2
                Exit For
            End If
        Next
        line = line.Replace("[", "(")
        line = line.Replace("]", ")")

        If original.Contains(".Set(Of ") Then
            split = line.Split(" ")
            If split.Length > 3 Then
                split(split.Length - 3) = split(split.Length - 3).Replace(",", ")")
                split(split.Length - 2) = " = " + split(split.Length - 2)
                split(split.Length - 2) = split(split.Length - 2).Replace(")", "")
                line = ""
                For i = 0 To split.Length - 1
                    line += split(i) + " "
                Next
                If line.EndsWith(") ;") Then line = line.Replace(") ;", ";")
            End If
        End If
            Return line
    End Function
    Public Function updateDeclaration(line As String) As String
        Dim original = line
        Dim testline = line.Replace(";", "")
        Dim tabs = countTabs(line)
        Dim split = testline.Trim.Split(" ")
        Dim varType As String = ""

        If split.Length < 3 Then Return line
        If split(2) = "As" Then
            split(3) = split(3).Replace("()", "[]")
            Select Case split(3)
                Case "New"
                    If split(4) = "List(Of" Then
                        split(5) = split(5).Replace(")", "")
                        split(5) = split(5).Replace("(", "")
                        varType = "vector<" + split(5) + ">"
                    Else
                        varType = split(4)
                    End If
                Case "Integer"
                    varType = "int"
                Case "Double"
                    varType = "double"
                Case "Single"
                    varType = "float"
                Case "Boolean"
                    varType = "bool"
                Case "cv.Mat"
                    varType = "Mat"
                Case "cv.Scalar"
                    varType = "Scalar"
                Case "cv.Rect"
                    varType = "Rect"
                Case "Byte"
                    varType = "unsigned char"
                Case "Short"
                    varType = "uint16"
                Case "cv.Rect"
                    varType = "Rect"
                Case "cv.Rect"
                    varType = "Rect"
                Case "cv.Point[]"
                    varType = "Mat"
                Case Else
                    varType = split(3)
            End Select

            line = tabs + varType + " " + split(1) + " "
            If split.Length > 4 Then
                If split(4) = "=" Then
                    For i = 4 To split.Length - 1
                        line += split(i) + " "
                    Next
                End If
            End If
            If line.EndsWith(" ") Then line = line.Substring(0, line.Length - 1)
            If line.EndsWith(";") = False Then line += ";"
        End If
        line = line.Replace("cv.", "")
        Return line
    End Function
End Module
