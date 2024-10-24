﻿Imports System.Runtime.InteropServices
Imports cvb = OpenCvSharp
Public Class Neighbors_Basics : Inherits TaskParent
    Public redC As New RedCloud_Basics
    Dim knn As New KNN_Basics
    Public runRedCloud As Boolean = False
    Public options As New Options_XNeighbors
    Public Sub New()
        desc = "Find all the neighbors with KNN"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standalone Or runRedCloud Then
            redC.Run(src)
            dst2 = redC.dst2
            labels = redC.labels
        End If

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDStable)
        Next
        knn.trainInput = New List(Of cvb.Point2f)(knn.queries)
        knn.Run(src)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nabs = knn.neighbors(i)
        Next

        If standalone Then
            task.setSelectedContour()
            dst3.SetTo(0)
            Dim ptCount As Integer
            For Each index In task.rc.nabs
                Dim pt = task.redCells(index).maxDStable
                If pt = task.rc.maxDStable Then
                    DrawCircle(dst2,pt, task.DotSize, black)
                Else
                    DrawCircle(dst2,pt, task.DotSize, task.HighlightColor)
                    ptCount += 1
                    If ptCount > options.xNeighbors Then Exit For
                End If
            Next
        End If
    End Sub
End Class







Public Class Neighbors_Intersects : Inherits TaskParent
    Public nPoints As New List(Of cvb.Point)
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Or src.Type <> cvb.MatType.CV_8U Then
            redC.Run(src)
            dst2 = redC.dst2
            src = task.cellMap
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
                    nPoints.Add(New cvb.Point(x, y))
                End If
            Next
        Next

        If standaloneTest() Then
            dst3 = task.color.Clone
            For Each pt In nPoints
                DrawCircle(dst2,pt, task.DotSize, task.HighlightColor)
                DrawCircle(dst3,pt, task.DotSize, cvb.Scalar.Yellow)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class









Public Class Neighbors_ColorOnly : Inherits TaskParent
    Dim corners As New Neighbors_Intersects
    Dim redC As New RedCloud_Cells
    Public Sub New()
        desc = "Find neighbors in a color only RedCloud cellMap"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        corners.Run(task.cellMap.Clone())
        For Each pt In corners.nPoints
            DrawCircle(dst2,pt, task.DotSize, task.HighlightColor)
        Next

        labels(2) = redC.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
    End Sub
End Class







Public Class Neighbors_Precise : Inherits TaskParent
    Public nabList As New List(Of List(Of Integer))
    Dim stats As New Cell_Basics
    Public redCells As List(Of rcData)
    Public runRedCloud As Boolean = False
    Dim redC As New RedCloud_Basics
    Public Sub New()
        cPtr = Neighbors_Open()
        If standaloneTest() Then task.gOptions.setDisplay1()
        desc = "Find the neighbors in a selected RedCloud cell"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Or runRedCloud Then
            redC.Run(src)
            dst2 = redC.dst2
            labels = redC.labels

            src = task.cellMap
            redCells = task.redCells
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        SetTrueText("Review the neighbors_Precise algorithm")

        'If nabCount > 0 Then
        '    Dim nabData = New cvb.Mat(nabCount, 1, cvb.MatType.CV_32SC2, Neighbors_NabList(cPtr))
        '    nabList.Clear()
        '    For i = 0 To redCells.Count - 1
        '        nabList.Add(New List(Of Integer))
        '    Next
        '    redCells(i).nab = nabList.Min()
        '    For i = 0 To nabCount - 1
        '        Dim pt = nabData.Get(Of cvb.Point)(i, 0)
        '        If nabList(pt.X).Contains(pt.Y) = False And pt.Y <> 0 Then
        '            nabList(pt.X).Add(pt.Y)
        '            redCells(pt.X).nabs.Add(pt.Y)
        '        End If
        '        If nabList(pt.Y).Contains(pt.X) = False And pt.X <> 0 Then
        '            nabList(pt.Y).Add(pt.X)
        '            redCells(pt.Y).nabs.Add(pt.X)
        '        End If
        '    Next
        '    nabList(0).Clear() ' neighbors to zero are not interesting (yet?)
        '    redCells(0).nabs.Clear() ' not interesting.

        '    If task.heartBeat And standaloneTest() Then
        '        stats.Run(task.color)

        '        strOut = stats.strOut
        '        If nabList(task.rc.index).Count > 0 Then
        '            strOut += "Neighbors: "
        '            dst1.SetTo(0)
        '            dst1(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        '            For Each index In nabList(task.rc.index)
        '                Dim rc = redCells(index)
        '                dst1(rc.rect).SetTo(rc.color, rc.mask)
        '                strOut += CStr(index) + ","
        '            Next
        '            strOut += vbCrLf
        '        End If
        '    End If
        '    SetTrueText(strOut, 3)
        'End If

        labels(3) = CStr(nabCount) + " neighbor pairs were found."
    End Sub
    Public Sub Close()
        Neighbors_Close(cPtr)
    End Sub
End Class