Imports cv = OpenCvSharp
Imports System.IO
Namespace VBClasses
    Public Class NR_CSV_Basics : Inherits TaskParent
        Public inputFile As String
        Public array(,) As String
        Public arrayList As New List(Of List(Of String))
        Dim readText() As String
        Dim variables() As String
        Public Sub New()
            Dim fileInput As New FileInfo(atask.homeDir + "Data/agaricus-lepiota.data")
            inputFile = fileInput.FullName
            readText = File.ReadAllLines(inputFile) ' user supplies the inputfile name.
            Dim variables = readText(0).Split(",")
            ReDim array(readText.Length - 1, variables.Length - 1)
            desc = "Read and prepare a .csv file"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            For i = 0 To array.GetUpperBound(0)
                variables = readText(i).Split(",")
                For j = 0 To array.GetUpperBound(1)
                    array(i, j) = variables(j)
                Next
            Next

            For i = 0 To array.GetUpperBound(1)
                arrayList.Add(New List(Of String))
                For j = 0 To array.GetUpperBound(0)
                    arrayList(i).Add(array(j, i))
                Next
            Next
            If standaloneTest() Then SetTrueText(inputFile + " is now loaded into the csv.array")

            ReDim array(0, 0)
        End Sub
    End Class
End Namespace