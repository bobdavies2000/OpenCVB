Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbor_Basics : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public runRedCflag As Boolean = False
    Public options As New Options_Neighbors
    Public Sub New()
        desc = "Find all the neighbors with KNN"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standalone Or runRedCflag Then dst2 = runRedColor(src, labels(2))

        knn.queries.Clear()
        For Each rc In task.redColor.rcList
            knn.queries.Add(rc.maxDist)
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(src)

        For Each rc In task.redColor.rcList
            For i = 0 To Math.Min(knn.neighbors.Count, options.neighbors) - 1
                rc.nabs.Add(knn.neighbors(rc.index)(i))
            Next
        Next

        If standalone Then
            task.setSelectedCell()
            dst3.SetTo(0)
            For Each index In task.rcD.nabs
                If index < task.redColor.rcList.Count Then
                    DrawCircle(dst2, task.redColor.rcList(index).maxDist, task.DotSize, task.highlight)
                End If
            Next
        End If
    End Sub
End Class







Public Class Neighbor_Intersects : Inherits TaskParent
    Public nPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or src.Type <> cv.MatType.CV_8U Then
            dst2 = runRedColor(src, labels(2))
            src = task.redColor.rcMap
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
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class









Public Class Neighbor_ColorOnly : Inherits TaskParent
    Dim corners As New Neighbor_Intersects
    Public Sub New()
        desc = "Find neighbors in a redColor cellMap"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedColor(src, labels(2))

        corners.Run(task.redColor.rcMap.Clone())
        For Each pt In corners.nPoints
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next

        labels(2) = task.redColor.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
    End Sub
End Class







Public Class Neighbor_Precise : Inherits TaskParent
    Public nabList As New List(Of List(Of Integer))
    Public rcList As List(Of rcData)
    Public runRedCflag As Boolean = False
    Public Sub New()
        cPtr = Neighbor_Open()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the neighbors in a selected RedCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or runRedCflag Then
            dst2 = runRedColor(src, labels(2))

            src = task.redColor.rcMap
            rcList = task.redColor.rcList
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbor_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        SetTrueText("Review the Neighbor_Precise algorithm")

        'If nabCount > 0 Then
        '    Dim nabData = New cv.Mat(nabCount, 1, cv.MatType.CV_32SC2, Neighbor_NabList(cPtr))
        '    nabList.Clear()
        '    For i = 0 To rcList.Count - 1
        '        nabList.Add(New List(Of Integer))
        '    Next
        '    rcList(i).nab = nabList.Min()
        '    For i = 0 To nabCount - 1
        '        Dim pt = nabData.Get(Of cv.Point)(i, 0)
        '        If nabList(pt.X).Contains(pt.Y) = False And pt.Y <> 0 Then
        '            nabList(pt.X).Add(pt.Y)
        '            rcList(pt.X).nabs.Add(pt.Y)
        '        End If
        '        If nabList(pt.Y).Contains(pt.X) = False And pt.X <> 0 Then
        '            nabList(pt.Y).Add(pt.X)
        '            rcList(pt.Y).nabs.Add(pt.X)
        '        End If
        '    Next
        '    nabList(0).Clear() ' neighbors to zero are not interesting (yet?)
        '    rcList(0).nabs.Clear() ' not interesting.

        '    If task.heartBeat And standaloneTest() Then
        '        Static stats As New XO_RedCell_Basics
        '        If stats Is Nothing Then stats = New XO_RedCell_Basics
        '        stats.Run(task.color)

        '        strOut = stats.strOut
        '        If nabList(task.rcD.index).Count > 0 Then
        '            strOut += "Neighbors: "
        '            dst1.SetTo(0)
        '            dst1(task.rcD.rect).SetTo(task.rcD.color, task.rcD.mask)
        '            For Each index In nabList(task.rcD.index)
        '                Dim rc = rcList(index)
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
        Neighbor_Close(cPtr)
    End Sub
End Class