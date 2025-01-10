Imports cv = OpenCvSharp
Imports System.IO

Public Class CSV_Basics : Inherits TaskParent
    Public inputFile As String
    Public array(,) As String
    Public arrayList As New List(Of List(Of String))
    Public Sub New()
        Dim fileInput As New FileInfo(task.HomeDir + "Data/agaricus-lepiota.data")
        inputFile = fileInput.FullName
        desc = "Read and prepare a .csv file"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim readText() As String = File.ReadAllLines(inputFile) ' user supplies the inputfile name.
        Dim variables = readText(0).Split(",")
        ReDim array(readText.Length - 1, variables.Length - 1)
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
    End Sub
End Class