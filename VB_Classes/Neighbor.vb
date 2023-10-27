Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbor_Basics : Inherits VB_Algorithm
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
                    If cellData.Contains(series) = False Then cellData.Add(series)
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
                    If nabList(index).Contains(jIndex) = False Then nabList(index).Add(jIndex)
                    If nabList(jIndex).Contains(index) = False Then nabList(jIndex).Add(index)
                Next
            Next
        Next

        labels(2) = CStr(cellCount) + " regions presents."
    End Sub
End Class









Public Class Neighbor_CPP : Inherits VB_Algorithm
    Public cellCount As Integer
    Public nabList() As List(Of Byte)
    Public Sub New()
        cPtr = Neighbor_Open()
        desc = "Find the list of neighbors for a cell using the cellmap in C++"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or src.Type <> cv.MatType.CV_8U Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            src = task.cellMap
            labels(2) = redC.labels(2)
        End If

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Neighbor_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.redCells.Count)
        handleSrc.Free()

        Dim count = Neighbor_Count(cPtr)
        If count > 0 Then
            Dim idList = New cv.Mat(count, 1, cv.MatType.CV_32S, Neighbor_List(cPtr))
            For i = 0 To count - 1
                Dim rcX = task.redCells(idList.Get(Of Integer)(i, 0))
                vbDrawContour(dst3(rcX.rect), rcX.contour, rcX.color, -1)
            Next
        End If
    End Sub
    Public Sub Close()
        Neighbor_Close(cPtr)
    End Sub
End Class





Module Neighbor_Module

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbor_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_List(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_Count(cPtr As IntPtr) As Integer
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer, cellCount As Integer) As Integer
    End Function

End Module








Public Class Neighbor_Corner : Inherits VB_Algorithm
    Public Sub New()
        labels = {"", "", "Grayscale", "dst3Label"}
        desc = "description"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
    End Sub
End Class
