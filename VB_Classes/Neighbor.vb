Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbor_Basics : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim knn As New KNN_Core
    Public Sub New()
        desc = "Find all the neighbors with KNN"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels = redC.labels

        knn.queries.Clear()
        For Each rc In redC.redCells
            knn.queries.Add(rc.maxDStable)
        Next
        knn.Run(src)

        For i = 0 To redC.redCells.Count - 1
            Dim rc = redC.redCells(i)
            rc.neighbors = knn.neighbors(i)
        Next

        setSelectedCell(redC.redCells, redC.cellMap)

        dst3.SetTo(0)
        Dim ptCount As Integer
        For Each index In task.rc.neighbors
            Dim pt = redC.redCells(index).maxDStable
            If pt = task.rc.maxDStable Then
                dst2.Circle(pt, task.dotSize, black, -1, task.lineType)
            Else
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                ptCount += 1
                If ptCount > 20 Then Exit For
            End If
        Next

        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class Neighbor_Core : Inherits VB_Algorithm
    Public nabList As New List(Of List(Of Integer))
    Dim stats As New Cell_Basics
    Public redCells As List(Of rcData)
    Public runRedCloud As Boolean = False
    Public Sub New()
        cPtr = Neighbors_Open()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        desc = "Find the neighbors in a selected RedCloud cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels = redC.labels

            src = redC.cellMap
            redCells = redC.redCells
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If nabCount > 0 Then
            Dim nabData = New cv.Mat(nabCount, 1, cv.MatType.CV_32SC2, Neighbor_NabList(cPtr))
            nabList.Clear()
            For i = 0 To redCells.Count - 1
                nabList.Add(New List(Of Integer))
            Next
            For i = 0 To nabCount - 1
                Dim pt = nabData.Get(Of cv.Point)(i, 0)
                If nabList(pt.X).Contains(pt.Y) = False And pt.Y <> 0 Then nabList(pt.X).Add(pt.Y)
                If nabList(pt.Y).Contains(pt.X) = False And pt.X <> 0 Then nabList(pt.Y).Add(pt.X)
            Next
            nabList(0).Clear() ' neighbors to zero are not interesting (yet?)

            If task.heartBeat And standaloneTest() Then
                stats.Run(task.color)

                strOut = stats.strOut
                If nabList(task.rc.index).Count > 0 Then
                    strOut += "Neighbors: "
                    dst1.SetTo(0)
                    dst1(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
                    For Each index In nabList(task.rc.index)
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
        Neighbors_Close(cPtr)
    End Sub
End Class






Public Class Neighbor_Intersects : Inherits VB_Algorithm
    Public nPoints As New List(Of cv.Point)
    Dim ePoints As New Neighbor_IntersectsImageEdge
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or src.Type <> cv.MatType.CV_8U Then
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

        If standaloneTest() Then
            dst3 = task.color.Clone
            For Each pt In nPoints
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                dst3.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class






Public Class Neighbor_IntersectsImageEdge : Inherits VB_Algorithm
    Public nPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the cell boundaries at the edge of the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
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
    Dim corners As New Neighbor_Intersects
    Dim redC As New RedCloud_Cells
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
    Dim corners As New Neighbor_Intersects
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







Public Class Neighbor_CoreTest : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim nabs As New Neighbor_Core
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
        dst3(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        For Each index In nabs.nabList(task.rc.index)
            Dim rc = redC.redCells(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
    End Sub
End Class







Public Class Neighbor_Precise : Inherits VB_Algorithm
    Dim nabs As New Neighbor_Core
    Public cellMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public redCells As New List(Of rcData)
    Public Sub New()
        nabs.runRedCloud = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Given a cell, find all the cells that touch it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        nabs.Run(src)
        dst1 = nabs.dst2
        labels(1) = nabs.labels(2)

        For Each index In nabs.nabList(task.rc.index)
            Dim rc = nabs.redCells(index)
            dst2.Circle(rc.maxDStable, task.dotSize, task.highlightColor, -1, task.lineType)
            strOut += CStr(index) + ","
        Next

        cellMap.SetTo(0)
        dst2.SetTo(0)
        dst3.SetTo(0)
        redCells.Clear()
        Dim count As Integer
        For Each rc In nabs.redCells
            rc.index = redCells.Count
            If nabs.nabList(rc.index).Count = 1 Then
                Dim r = rc.rect
                If r.X <> 0 And r.Y <> 0 And r.X + r.Width < dst2.Width - 1 And r.Y + r.Height < dst2.Height - 1 Then
                    count += 1
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    cellMap(rc.rect).SetTo(rc.index, rc.mask)
                    Continue For
                End If
            End If
            rc.neighbors = New List(Of Integer)(nabs.nabList(rc.index))
            redCells.Add(rc)

            cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
        Next

        setSelectedCell(redCells, cellMap)
        labels(2) = $"{redCells.Count} cells were identified."
        labels(3) = $"{count} cells had only one neighbor and were merged with that neighbor."
    End Sub
End Class
