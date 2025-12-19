Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Neighbor_Basics : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public runRedCflag As Boolean = False
        Public options As New Options_Neighbors
        Public Sub New()
            desc = "Find all the neighbors with KNN"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standalone Or runRedCflag Then dst2 = runRedList(src, labels(2))

            knn.queries.Clear()
            For Each rc In taskAlg.redList.oldrclist
                knn.queries.Add(rc.maxDist)
            Next
            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
            knn.Run(src)

            For Each rc In taskAlg.redList.oldrclist
                For i = 0 To Math.Min(knn.neighbors.Count, options.neighbors) - 1
                    rc.nabs.Add(knn.neighbors(rc.index)(i))
                Next
            Next

            If standalone Then
                RedList_Basics.setSelectedCell()
                dst3.SetTo(0)
                For Each index In taskAlg.oldrcD.nabs
                    If index < taskAlg.redList.oldrclist.Count Then
                        DrawCircle(dst2, taskAlg.redList.oldrclist(index).maxDist, taskAlg.DotSize, taskAlg.highlight)
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
                dst2 = runRedList(src, labels(2))
                src = taskAlg.redList.rcMap
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
                dst3 = taskAlg.color.Clone
                For Each pt In nPoints
                    DrawCircle(dst2, pt, taskAlg.DotSize, taskAlg.highlight)
                    DrawCircle(dst3, pt, taskAlg.DotSize, cv.Scalar.Yellow)
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
            dst2 = runRedList(src, labels(2))

            corners.Run(taskAlg.redList.rcMap.Clone())
            For Each pt In corners.nPoints
                DrawCircle(dst2, pt, taskAlg.DotSize, taskAlg.highlight)
            Next

            labels(2) = taskAlg.redList.labels(2) + " and " + CStr(corners.nPoints.Count) + " cell intersections"
        End Sub
    End Class







    Public Class Neighbor_Precise : Inherits TaskParent
        Public nabList As New List(Of List(Of Integer))
        Public oldrclist As List(Of oldrcData)
        Public runRedCflag As Boolean = False
        Public Sub New()
            cPtr = Neighbor_Open()
            If standalone Then taskAlg.gOptions.displayDst1.Checked = True
            desc = "Find the neighbors in a selected RedCell"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Or runRedCflag Then
                dst2 = runRedList(src, labels(2))

                src = taskAlg.redList.rcMap
                oldrclist = taskAlg.redList.oldrclist
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
            '    For i = 0 To oldrclist.Count - 1
            '        nabList.Add(New List(Of Integer))
            '    Next
            '    oldrclist(i).nab = nabList.Min()
            '    For i = 0 To nabCount - 1
            '        Dim pt = nabData.Get(Of cv.Point)(i, 0)
            '        If nabList(pt.X).Contains(pt.Y) = False And pt.Y <> 0 Then
            '            nabList(pt.X).Add(pt.Y)
            '            oldrclist(pt.X).nabs.Add(pt.Y)
            '        End If
            '        If nabList(pt.Y).Contains(pt.X) = False And pt.X <> 0 Then
            '            nabList(pt.Y).Add(pt.X)
            '            oldrclist(pt.Y).nabs.Add(pt.X)
            '        End If
            '    Next
            '    nabList(0).Clear() ' neighbors to zero are not interesting (yet?)
            '    oldrclist(0).nabs.Clear() ' not interesting.

            '    If taskAlg.heartBeat And standaloneTest() Then
            '        Static stats As New XO_RedCell_Basics
            '        If stats Is Nothing Then stats = New XO_RedCell_Basics
            '        stats.Run(taskAlg.color)

            '        strOut = stats.strOut
            '        If nabList(taskAlg.oldrcD.index).Count > 0 Then
            '            strOut += "Neighbors: "
            '            dst1.SetTo(0)
            '            dst1(taskAlg.oldrcD.rect).SetTo(taskAlg.oldrcD.color, taskAlg.oldrcD.mask)
            '            For Each index In nabList(taskAlg.oldrcD.index)
            '                Dim rc = oldrclist(index)
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
End Namespace