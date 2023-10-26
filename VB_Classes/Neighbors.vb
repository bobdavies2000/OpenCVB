Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbors_Basics : Inherits VB_Algorithm
    Public cellCount As Integer
    Public nabList() As List(Of Byte)
    Public Sub New()
        desc = "Find neighbors for each cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(task.color)
            dst2 = redC.dst2
            src = task.cellMap
            cellCount = task.redCells.Count
        End If

        Dim samples(src.Total - 1) As Byte
        Marshal.Copy(src.Data, samples, 0, samples.Length)

        Dim nPoints As New List(Of cv.Point)
        Dim w = dst2.Width
        Dim cellData As New List(Of String)
        Dim kSize As Integer = 2
        For y = 0 To dst1.Height - kSize
            For x = 0 To dst1.Width - kSize
                Dim nabs As New SortedList(Of Byte, Byte)
                For yy = y To y + kSize - 1
                    For xx = x To x + kSize - 1
                        Dim val = samples(yy * w + xx)
                        If val >= 1 Then If nabs.ContainsKey(val) = False Then nabs.Add(val, 0)
                    Next
                Next
                If nabs.Count >= 2 Then
                    Dim series As String = ""
                    For Each ele In nabs
                        series += CStr(ele.Key) + " "
                    Next
                    If cellData.Contains(series) = False Then
                        cellData.Add(series)
                        nPoints.Add(New cv.Point(x + 1, y + 1))
                    End If
                End If
            Next
        Next

        ReDim nabList(cellCount)
        For i = 0 To nabList.Count - 1
            nabList(i) = New List(Of Byte)
        Next

        For Each n In cellData
            Dim split = Trim(n).Split(" ")
            For i = 0 To split.Length - 1
                Dim index = CInt(split(0))
                For j = i + 1 To split.Length - 1
                    Dim jIndex = CInt(split(j))
                    If nabList(index).Contains(jIndex) = False Then
                        nabList(index).Add(jIndex)
                        nabList(jIndex).Add(index)
                    End If
                Next
            Next
        Next

        labels(2) = CStr(cellCount) + " regions presents."
    End Sub
End Class