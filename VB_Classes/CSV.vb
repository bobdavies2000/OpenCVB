Imports cvb = OpenCvSharp
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
    Public Overrides sub runAlg(src As cvb.Mat)
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









'Public Class CSV_Excel : Inherits TaskParent
'    Public inputFile As String
'    Public dataTable As DataTable
'    Public Sub New()
'        inputFile = task.HomeDir + "Data\examples.xls" ' default input file when run standaloneTest()
'        desc = "Read an Excel file"
'    End Sub
'    Public Overrides sub runAlg(src As cvb.Mat)
'        Dim reader As IExcelDataReader
'        Dim stream = File.Open(inputFile, FileMode.Open, FileAccess.Read)
'        reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream)
'        Dim conf = New ExcelDataSetConfiguration
'        Dim configureDataTable = New ExcelDataTableConfiguration

'        Dim dataSet = reader.AsDataSet(conf)
'        dataTable = dataSet.Tables(0)
'        If standaloneTest() Then Dim array = dataTable.ToJagged(Of Double)("Column0", "Column1")
'        SetTrueText("Input file: " + inputFile + vbCrLf + "Can now be read with the following:" + vbCrLf + vbCrLf +
'                    "dim array = excel.dataTable.ToJagged(of Double)(column1 label, column2 label)")
'        reader.Close()
'    End Sub
'End Class
