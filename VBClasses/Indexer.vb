Imports cv = OpenCvSharp
Public Class Indexer_Basics : Inherits TaskParent
    Public prep As New RedPrep_Core
    Public Sub New()
        desc = "Find edges in the reduced point cloud output using indexers"
    End Sub
    Public Shared Function buildEdges(Input As cv.Mat) As cv.Mat
        Dim horizontalMat = Input.Clone
        Dim indexer1 As cv.MatIndexer(Of Byte) = horizontalMat.GetGenericIndexer(Of Byte)()
        For y = 0 To horizontalMat.Rows - 1
            For x = 1 To horizontalMat.Cols - 1
                If indexer1(y, x) = indexer1(y, x - 1) Then indexer1(y, x - 1) = 0
            Next
        Next

        Dim verticalMat = Input.Clone
        Dim indexer2 As cv.MatIndexer(Of Byte) = verticalMat.GetGenericIndexer(Of Byte)()
        For x = 0 To verticalMat.Cols - 1
            For y = 1 To verticalMat.Rows - 1
                If indexer2(y, x) = indexer2(y - 1, x) Then indexer2(y - 1, x) = 0
            Next
        Next

        Input.SetTo(0)
        Input.SetTo(255, horizontalMat)
        Input.SetTo(255, verticalMat)
        Return Input
    End Function

    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then
            prep.Run(src)
            dst2 = prep.dst2.Clone
            labels(2) = prep.labels(2)
        Else
            dst2 = src
        End If

        dst2 = buildEdges(dst2)
        dst2.SetTo(255, task.noDepthMask)
    End Sub
End Class





Public Class Indexer_Corners : Inherits TaskParent
    Dim vals As New List(Of Byte)
    Dim indexBasics As New Indexer_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find the corners in the RedPrep XY data and clip.  Identical to Indexer_Basics"
    End Sub
    Private Sub addVal(val As Byte)
        If vals.Contains(val) = False Then vals.Add(val)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        indexBasics.Run(src)
        dst2 = indexBasics.dst2
        labels(2) = indexBasics.labels(2)

        dst3 = indexBasics.prep.dst2.Clone
        Dim indexer As cv.MatIndexer(Of Byte) = dst3.GetGenericIndexer(Of Byte)()
        Dim rectList As New List(Of cv.Rect)
        For y = 1 To dst3.Rows - 3
            For x = 1 To dst3.Cols - 3
                vals.Clear()
                vals.Add(indexer(y - 1, x - 1))
                addVal(indexer(y - 1, x))
                addVal(indexer(y - 1, x + 1))
                addVal(indexer(y, x - 1))
                addVal(indexer(y, x))
                addVal(indexer(y, x + 1))
                addVal(indexer(y + 1, x - 1))
                addVal(indexer(y + 1, x))
                addVal(indexer(y + 1, x + 1))
                If vals.Count > 2 Then rectList.Add(New cv.Rect(x - 1, y - 1, 3, 3))
            Next
        Next

        Dim count As Integer
        dst1.SetTo(0)
        For Each r In rectList
            Dim val = indexer(r.TopLeft.Y, r.TopLeft.X)
            If val <> 0 Then
                dst1(r).SetTo(255)
                dst3(r).SetTo(0)
                count += 1
            End If
        Next

        dst2.SetTo(255, dst1)
        labels(3) = CStr(count) + " corners found"
    End Sub
End Class
