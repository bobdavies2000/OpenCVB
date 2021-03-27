Imports System.IO
Module VB_EditorMain
    Dim changeLines As Integer
    Private Function makeChange(line As String) As String
        If line.Contains("") Then
            Console.WriteLine(line)
            line = line.Replace("setDescription(", "task.desc = ")
            line = Mid(line, 1, Len(line) - 1)
            Console.WriteLine("Change to: " + line)
            changeLines += 1
        End If
        Return line
    End Function
    Private Function deleteLine(line As String) As Boolean
        If line.Contains("MyBase.Finish()") Then
            Console.WriteLine("Deleting line: " + line)
            changeLines += 1
            Return True
        End If
        Return False
    End Function

    Private Function insertLine(line As String) As Boolean
        Static insideRunFunction As Boolean
        'If Trim(line) = "End Sub" And insideRunFunction Then
        If insideRunFunction Then
            Console.WriteLine(vbTab + vbTab + "If task.intermediateReview = caller Then task.intermediateObject = Me")
            changeLines += 1
            insideRunFunction = False
            Return True
        End If
        If InStr(line, "Public Sub Run()") Then
            insideRunFunction = True
        End If
        'Console.WriteLine(line)
        Return False
    End Function
    Sub Main()
        ' Regular expression are great but can be too complicated.  This app is just a simpler way to make global changes that 
        ' would normally be accomplished with regular expressions.
        ' There are 3 operations - delete a line, change a line, or insertline.
        ' The first loop displays what the change would look like
        ' The second loop makes the change
        ' Run without the second loop until you see the desired results then run the second loop.
        Dim VBcodeDir As New DirectoryInfo(CurDir() + "/../../VB_Classes")
        Dim fileEntries As String() = Directory.GetFiles(VBcodeDir.FullName)

        Dim changeFiles As New List(Of String)
        For Each fileName In fileEntries
            Dim nextFile As New System.IO.StreamReader(fileName)
            Dim saveChangeLines = changeLines
            While nextFile.Peek() <> -1
                Dim line As String
                line = nextFile.ReadLine()
                ' deleteLine(line)
                'makeChange(line)
                insertLine(line)
            End While
            nextFile.Close()
            If saveChangeLines <> changeLines Then changeFiles.Add(fileName)
        Next
        Console.WriteLine(CStr(changeLines) + " matching lines found in " + CStr(changeFiles.Count) + " files")

        Dim response = InputBox("Respond 'Yes' to make the changes.", "Make Changes?", "")
        If response = "Yes" Then
            changeLines = 0
            For Each filename In changeFiles
                Dim sr = New StreamReader(filename)
                Dim code As String = sr.ReadToEnd
                sr.Close()
                Dim lines = code.Split(vbCrLf)
                sr = New StreamReader(filename)
                For i = 0 To lines.Count - 1
                    lines(i) = sr.ReadLine()
                Next
                If lines.Count = 1 Then
                    lines = code.Split(vbLf) ' just in case they don't have CR.
                End If
                sr.Close()
                'For i = 0 To lines.Count - 1
                '    lines(i) = makeChange(Trim(lines(i)))
                'Next

                Dim sw = New StreamWriter(filename)
                For i = 0 To lines.Count - 1
                    If insertLine(lines(i)) Then
                        sw.WriteLine(vbTab + vbTab + "If task.intermediateReview = caller Then task.intermediateObject = Me")
                    End If
                    sw.WriteLine(lines(i))
                    'If deleteLine(lines(i)) Then
                    '    Console.WriteLine("Deleting: " + lines(i))
                    'Else
                    '    sw.Write(lines(i))
                    'End If
                Next
                sw.Close()
            Next
        End If

    End Sub
End Module
