﻿Imports System.IO
Module UIranking
    Sub Main()
        Dim rankingInput As New FileInfo("../../Data/rankings.txt")
        Dim sr = New StreamReader(rankingInput.FullName)
        Dim code As String = sr.ReadToEnd
        sr.Close()
        Dim lines() As String = code.Split(vbLf)
        Dim apiList As New List(Of String)
        Dim apiReferences As New List(Of String)
        Dim apiKeywords As New List(Of String)
        For i = 0 To lines.Count - 1
            lines(i) = lines(i).Replace(vbCr, "").Replace(vbLf, "")
            If lines(i).Contains("Python_Run") = False Then
                Dim split() = lines(i).Split(",")
                apiList.Add(split(0))
                apiKeywords.Add(split(0))
                If lines(i).Length > split(0).Length Then apiReferences.Add(lines(i).Substring(split(0).Length + 1)) Else apiReferences.Add("")
            End If
        Next
        Dim apiRank(apiList.Count - 1) As Integer
        For i = 0 To apiReferences.Count - 1
            Dim split() = apiReferences(i).Split(",")
            If split.Length > 1 Then
                For j = 0 To split.Length - 1
                    If apiList.Contains(split(j)) Then
                        Dim index = apiList.IndexOf(split(j))
                        If apiKeywords(index).Contains(apiList(i)) = False Then
                            apiKeywords(index) += "," + apiList(i)
                            apiRank(index) += 1
                        End If
                    End If
                Next
            End If
        Next

        Dim maxRank = apiRank.Max
        For i = 0 To apiRank.Count - 1
            apiRank(i) = CInt(apiRank(i) * 10 / maxRank)
        Next

        Dim rankList As String
        Dim sw = New StreamWriter("../../Data/RankList.txt")
        For i = 1 To 9
            rankList = ""
            For j = 0 To apiRank.Count - 1
                If apiRank(j) = i Then
                    If rankList = "" Then rankList = apiList(j) Else rankList += "," + apiList(j)
                End If
            Next
            If rankList <> "" Then sw.WriteLine("<Reuse Rank " + CStr(i) + ">," + rankList)
        Next
        sw.Close()

        sw = New StreamWriter("../../Data/OpenCVBKeywords.txt")
        For i = 0 To apiKeywords.Count - 1
            Dim split = apiKeywords(i).Split(",")
            If split.Length > 1 Then sw.WriteLine(apiKeywords(i))
        Next
        sw.Close()
        Console.WriteLine("Read in " + CStr(lines.Count))
    End Sub
End Module