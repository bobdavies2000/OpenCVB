﻿Imports  System.IO
Module UIranking
    Sub Main()
        Dim rankingInput As New FileInfo("../../Data/AlgorithmMapToOpenCV.txt")
        Dim rankList As New FileInfo("../../Data/RankList.txt")
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

        Dim algorithmRank As New SortedList(Of String, String)

        For i = 0 To algorithms.Count - 1
            Dim name = algorithms.ElementAt(i).Key
            Dim key = Format(algorithms.ElementAt(i).Value, "0000") + name
            algorithmRank.Add(key, name)
        Next

        Dim sw = New StreamWriter(rankList.FullName)
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