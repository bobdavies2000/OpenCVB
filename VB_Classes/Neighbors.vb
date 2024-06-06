Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbors_Basics : Inherits VB_Parent
    Public redC As New RedCloud_Basics
    Dim knn As New KNN_Core
    Public runRedCloud As Boolean = False
    Public options As New Options_XNeighbors
    Public Sub New()
        desc = "Find all the neighbors with KNN"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Or runRedCloud Then
            redC.Run(src)
            dst2 = redC.dst2
            labels = redC.labels
        End If

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDStable)
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(src)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nabs = knn.neighbors(i)
        Next

        If standalone Then
            setSelectedContour()
            dst3.SetTo(0)
            Dim ptCount As Integer
            For Each index In task.rc.nabs
                Dim pt = task.redCells(index).maxDStable
                If pt = task.rc.maxDStable Then
                    drawCircle(dst2,pt, task.dotSize, black)
                Else
                    drawCircle(dst2,pt, task.dotSize, task.highlightColor)
                    ptCount += 1
                    If ptCount > options.xNeighbors Then Exit For
                End If
            Next
        End If
    End Sub
End Class







Public Class Neighbors_Intersects : Inherits VB_Parent
    Public nPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Or src.Type <> cv.MatType.CV_8U Then
            Static redC As New RedCloud_Basics
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
                    nPoints.Add(New cv.Point(x, y))
                End If
            Next
        Next

        If standaloneTest() Then
            dst3 = task.color.Clone
            For Each pt In nPoints
                drawCircle(dst2,pt, task.dotSize, task.highlightColor)
                drawCircle(dst3,pt, task.dotSize, cv.Scalar.Yellow)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class









Public Class Neighbors_ColorOnly : Inherits VB_Parent
    Dim corners As New Neighbors_Intersects
    Dim redC As New RedCloud_Cells
    Public Sub New()
        desc = "Find neighbors in a color only RedCloud cellMap"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2

        corners.Run(task.cellMap.Clone())
        For Each pt In corners.nPoints
            drawCircle(dst2,pt, task.dotSize, task.highlightColor)
        Next

        labels(2) = redC.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
    End Sub
End Class









Public Class Neighbors_PreciseTest : Inherits VB_Parent
    Dim nabs As New Neighbors_Precise
    Public Sub New()
        nabs.runRedCloud = True
        desc = "Test Neighbors_Precise to show how to use it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        setTrueText("Review the neighbors_Precise algorithm")
        'nabs.Run(src)
        'dst2 = nabs.dst2
        'labels(2) = nabs.labels(2)
        'If nabs.redCells.Count <= 1 Then Exit Sub

        'dst3.SetTo(0)
        'dst3(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        'For Each index In nabs.nabList(task.rc.index)
        '    Dim rc = nabs.redCells(index)
        '    dst3(rc.rect).SetTo(rc.color, rc.mask)
        'Next
    End Sub
End Class







Public Class Neighbors_Precise : Inherits VB_Parent
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

            src = task.cellMap
            redCells = task.redCells
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        setTrueText("Review the neighbors_Precise algorithm")

        'If nabCount > 0 Then
        '    Dim nabData = New cv.Mat(nabCount, 1, cv.MatType.CV_32SC2, Neighbors_NabList(cPtr))
        '    nabList.Clear()
        '    For i = 0 To redCells.Count - 1
        '        nabList.Add(New List(Of Integer))
        '    Next
        '    redCells(i).nab = nabList.Min()
        '    For i = 0 To nabCount - 1
        '        Dim pt = nabData.Get(Of cv.Point)(i, 0)
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
        '    setTrueText(strOut, 3)
        'End If

        labels(3) = CStr(nabCount) + " neighbor pairs were found."
    End Sub
    Public Sub Close()
        Neighbors_Close(cPtr)
    End Sub
End Class