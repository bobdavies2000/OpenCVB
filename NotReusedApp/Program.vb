Imports System.IO
Imports System.Runtime
Module NotReused
    Sub Main()
        Dim basePath As String = "c:\_src\OpenCVB\data\notreused.txt"
        Dim lines() As String = File.ReadAllLines(basePath)

        Dim nrList As New List(Of String)

        For i = 0 To lines.Count - 1
            Dim alg = lines(i).Trim(" ")
            Dim split = alg.Split("_")
            Dim vbfile = "c:\_src\OpenCVB\VBClasses\" + split(0) + ".vb"
            Dim text As String = File.ReadAllText(vbfile)
            text = text.Replace(alg, "NF_" + alg)
            File.WriteAllText(vbfile, text)
        Next

        For Each nr In nrList
            If nr.StartsWith("NR_") = False Then Debug.WriteLine(nr)
        Next

        Console.WriteLine()
        Console.WriteLine("Press any key to exit...")
        Console.ReadKey()
    End Sub
End Module
