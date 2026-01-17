Imports System.IO
Imports System.Runtime
Module NotReused
    Sub Main()
        Dim basePath As String = "c:\_src\OpenCVB\data\AvailableAlgorithms.txt"

        If Not File.Exists(basePath) Then
            Console.WriteLine("File not found: " & basePath)
            Return
        End If

        Dim lines() As String = File.ReadAllLines(basePath)

        Dim vbFiles As String() = Directory.GetFiles("c:\_src\OpenCVB\VBClasses", "*.vb", SearchOption.TopDirectoryOnly)

        Dim nrList As New List(Of String)

        Dim hitAlg As Boolean
        For i = 0 To lines.Count - 1
            Dim alg = " As New " + lines(i).Trim(" ")
            hitAlg = False
            For Each vbFile As String In vbFiles
                Dim text As String = File.ReadAllText(vbFile)

                If text.IndexOf(alg, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    hitAlg = True
                    Exit For
                End If
            Next
            If hitAlg = False Then nrList.Add(lines(i).Trim(" "))
        Next

        For Each nr In nrList
            If nr.StartsWith("NR_") = False Then Debug.WriteLine(nr)
        Next

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub
End Module
