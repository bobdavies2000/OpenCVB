Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Neighbors_Basics : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public runRedCloud As Boolean = False
    Public options As New Options_XNeighbors
    Public Sub New()
        desc = "Find all the neighbors with KNN"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If standalone Or runRedCloud Then dst2 = runRedC(src, labels(2))

        knn.queries.Clear()
        For Each rc In task.rcList
            knn.queries.Add(rc.maxDStable)
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(src)

        For i = 0 To task.rcList.Count - 1
            Dim rc = task.rcList(i)
            rc.nabs = knn.neighbors(i)
        Next

        If standalone Then
            task.setSelectedCell()
            dst3.SetTo(0)
            Dim ptCount As Integer
            For Each index In task.rc.nabs
                Dim pt = task.rcList(index).maxDStable
                If pt = task.rc.maxDStable Then
                    DrawCircle(dst2, pt, task.DotSize, black)
                Else
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
                    ptCount += 1
                    If ptCount > options.xNeighbors Then Exit For
                End If
            Next
        End If
    End Sub
End Class







Public Class Neighbors_Intersects : Inherits TaskParent
    Public nPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Find the corner points where multiple cells intersect."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or src.Type <> cv.MatType.CV_8U Then
            dst2 = runRedC(src, labels(2))
            src = task.rcMap
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
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.Yellow)
            Next
        End If

        labels(3) = CStr(nPoints.Count) + " intersections with 3 or more cells were found"
    End Sub
End Class









Public Class Neighbors_ColorOnly : Inherits TaskParent
    Dim corners As New Neighbors_Intersects
    Public Sub New()
        desc = "Find neighbors in a redColor cellMap"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        corners.Run(task.rcMap.Clone())
        For Each pt In corners.nPoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        labels(2) = task.redC.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
    End Sub
End Class







Public Class Neighbors_Precise : Inherits TaskParent
    Public nabList As New List(Of List(Of Integer))
    Dim stats As New Cell_Basics
    Public rcList As List(Of rcData)
    Public runRedCloud As Boolean = False
    Public Sub New()
        cPtr = Neighbors_Open()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Find the neighbors in a selected RedCloud cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Or runRedCloud Then
            dst2 = runRedC(src, labels(2))

            src = task.rcMap
            rcList = task.rcList
        End If

        Dim mapData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, mapData, 0, mapData.Length)
        Dim handleSrc = GCHandle.Alloc(mapData, GCHandleType.Pinned)
        Dim nabCount = Neighbors_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()
        SetTrueText("Review the neighbors_Precise algorithm")

        'If nabCount > 0 Then
        '    Dim nabData = New cv.Mat(nabCount, 1, cv.MatType.CV_32SC2, Neighbors_NabList(cPtr))
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
        '        stats.Run(task.color)

        '        strOut = stats.strOut
        '        If nabList(task.rc.index).Count > 0 Then
        '            strOut += "Neighbors: "
        '            dst1.SetTo(0)
        '            dst1(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        '            For Each index In nabList(task.rc.index)
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
        Neighbors_Close(cPtr)
    End Sub
End Class