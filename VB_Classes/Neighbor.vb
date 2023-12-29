Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbor_Basics : Inherits VB_Algorithm
    Public nabList As New List(Of List(Of Byte))
    Dim stats As New Cell_Basics
    Public redCells As List(Of rcData)
    Public Sub New()
        cPtr = Neighbor_Map_Open()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Find all the neighbors in a RedCloud cellmap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            src = redC.cellMap
            redCells = redC.redCells
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbor_Map_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If nabCount > 0 Then
            Dim nabData = New cv.Mat(nabCount, 1, cv.MatType.CV_32SC2, Neighbor_NabList(cPtr))
            nabList.Clear()
            For i = 0 To redCells.Count - 1
                nabList.Add(New List(Of Byte))
            Next
            For i = 0 To nabCount - 1
                Dim pt = nabData.Get(Of cv.Point)(i, 0)
                If nabList(pt.X).Contains(pt.Y) = False Then nabList(pt.X).Add(pt.Y)
                If nabList(pt.Y).Contains(pt.X) = False Then nabList(pt.Y).Add(pt.X)
            Next
            nabList(0).Clear()

            If heartBeat() And standalone Then
                stats.Run(task.color)

                strOut = stats.strOut
                If nabList(task.rcSelect.index).Count > 0 Then
                    strOut += "Neighbors: "
                    dst1.SetTo(0)
                    dst1(task.rcSelect.rect).SetTo(task.rcSelect.color, task.rcSelect.mask)
                    For Each index In nabList(task.rcSelect.index)
                        Dim rc = redCells(index)
                        dst1(rc.rect).SetTo(rc.color, rc.mask)
                        strOut += CStr(index) + ","
                    Next
                    strOut += vbCrLf
                End If
            End If
            setTrueText(strOut, 3)
        End If

        labels(3) = CStr(nabCount) + " neighbor pairs were found."
    End Sub
    Public Sub Close()
        Neighbor_Map_Close(cPtr)
    End Sub
End Class






Public Class Neighbor_Corners : Inherits VB_Algorithm
    Public nPoints As New List(Of cv.Point)
    Dim ePoints As New Neighbor_ImageEdges
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or src.Type <> cv.MatType.CV_8U Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            src = redC.cellMap
            labels(2) = redC.labels(2)
        End If

        Dim samples(src.Total - 1) As Byte
        Marshal.Copy(src.Data, samples, 0, samples.Length)

        Dim w = dst2.Width
        nPoints.Clear()
        Dim kSize As Integer = 2
        For y = 0 To dst1.Height - kSize
            For x = 0 To dst1.Width - kSize
                Dim nabs As New SortedList(Of Byte, Byte)
                For yy = y To y + kSize - 1
                    For xx = x To x + kSize - 1
                        Dim val = samples(yy * w + xx)
                        If val = 0 And removeZeroNeighbors Then Continue For
                        If nabs.ContainsKey(val) = False Then nabs.Add(val, 0)
                    Next
                Next
                If nabs.Count > 2 Then
                    nPoints.Add(New cv.Point(x, y))
                End If
            Next
        Next

        ePoints.Run(src)
        For Each pt In ePoints.nPoints
            nPoints.Add(pt)
        Next

        If standalone Then
            dst3 = task.color.Clone
            For Each pt In nPoints
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class






Module Neighbor_Module
    Public removeZeroNeighbors As Boolean = True

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbors_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_CellData(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_Points(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbors_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function





    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Neighbor2_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_Points(cPtr As IntPtr) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Neighbor2_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Integer, cols As Integer) As Integer
    End Function

End Module






Public Class Neighbor_ImageEdges : Inherits VB_Algorithm
    Public nPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the cell boundaries at the edge of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            src = redC.cellMap
            labels(2) = redC.labels(2)
        End If

        nPoints.Clear()
        For i = 0 To 3
            Dim rowCol As cv.Mat = Choose(i + 1, src.Row(0).Clone, src.Row(dst2.Height - 1).Clone, src.Col(0).Clone, src.Col(dst2.Width - 1).Clone)
            Dim data(rowCol.Total - 1) As Byte
            Marshal.Copy(rowCol.Data, data, 0, data.Length)

            Dim ptBase As cv.Point, pt As cv.Point
            ptBase.X = Choose(i + 1, -1, -1, 0, dst2.Width - 1)
            ptBase.Y = Choose(i + 1, 0, dst2.Height - 1, -1, -1)
            For j = 1 To data.Count - 1
                If (data(j) = 0 Or data(j - 1) = 0) And removeZeroNeighbors Then Continue For
                If data(j) <> data(j - 1) Then
                    pt.X = If(ptBase.X = -1, j, ptBase.X)
                    pt.Y = If(ptBase.Y = -1, j, ptBase.Y)
                    nPoints.Add(pt)
                End If
            Next
        Next

        dst2.SetTo(0)
        For Each pt In nPoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
    End Sub
End Class








Public Class Neighbor_ColorOnly : Inherits VB_Algorithm
    Dim corners As New Neighbor_Corners
    Dim redC As New RedColor_Cells
    Public Sub New()
        desc = "Find neighbors in a color only RedCloud cellMap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        corners.Run(redC.redC.cellMap.Clone())
        For Each pt In corners.nPoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        labels(2) = redC.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
    End Sub
End Class









Public Class Neighbor_StableMax : Inherits VB_Algorithm
    Dim stable As New Cell_StableMax
    Dim corners As New Neighbor_Corners
    Public Sub New()
        desc = "Find neighbors in the RedCloud_StableMax redCloud cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stable.Run(src)
        dst2 = stable.dst2
        labels(2) = stable.labels(2)

        corners.Run(stable.cellMap)

        dst3 = task.color.Clone
        For Each pt In corners.nPoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Next

        labels(3) = corners.labels(3)
    End Sub
End Class







Public Class Neighbor_Flood : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Identify the floodPoint Neighbor of the selected cell - NOTE: this does not provide a consistent neighbor"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        Dim rc = task.rcSelect
        If rc.floodPoint.X > 0 Then
            Dim rcNeighbor = redC.redCells(redC.cellMap.Get(Of Byte)(rc.floodPoint.Y, rc.floodPoint.X - 1))
            vbDrawContour(dst2(rcNeighbor.rect), rcNeighbor.contour, cv.Scalar.White, task.lineWidth)
        End If
        setTrueText("This does not provide a consistent neighbor value.  See Neighbor_Map_CPP", 3)
    End Sub
End Class






Public Class Neighbor_BasicsTest : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim nabs As New Neighbor_Basics
    Public Sub New()
        desc = "Test Neighbor_Basics to show how to use it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)
        If redC.redCells.Count <= 1 Then Exit Sub

        nabs.redCells = redC.redCells
        nabs.Run(redC.cellMap)

        dst3.SetTo(0)
        dst3(task.rcSelect.rect).SetTo(task.rcSelect.color, task.rcSelect.mask)
        For Each index In nabs.nabList(task.rcSelect.index)
            Dim rc = redC.redCells(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class

