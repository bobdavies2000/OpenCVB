Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class KNNLine_Basics : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public results(,) As Integer
    Public Sub New()
        labels(2) = "The line's end points or center closest to the mouse is highlighted."
        desc = "Use KNN to determine which line is being selected with mouse."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        dst2 = task.lines.dst2.Clone
        knn.trainInput.Clear()
        knn.queries.Clear()
        For Each lp In task.lines.lpList
            knn.trainInput.Add(lp.p1)
            knn.trainInput.Add(lp.ptCenter)
            knn.trainInput.Add(lp.p2)
        Next

        knn.queries.Add(task.mouseMovePoint)
        knn.Run(emptyMat)

        results = knn.result

        If standaloneTest() Then
            Dim index = Math.Floor(results(0, 0) / 3)
            Dim lpNext = task.lines.lpList(index)
            dst2.Line(lpNext.p1, lpNext.p2, task.highlight, task.lineWidth * 3, cv.LineTypes.AntiAlias)
        End If
    End Sub
End Class






Public Class KNNLine_Query : Inherits TaskParent
    Dim knnLine As New KNNLine_Basics
    Public Sub New()
        desc = "Query the KNN results for the nearest line to the mouse."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        knnLine.Run(src)
        dst2 = knnLine.dst2
        labels(2) = knnLine.labels(2)

        Dim index = Math.Floor(knnLine.results(0, 0) / 3)
        Dim lpNext = task.lines.lpList(index)
        dst2.Line(lpNext.p1, lpNext.p2, task.highlight, task.lineWidth * 3, cv.LineTypes.AntiAlias)
    End Sub
End Class





Public Class KNNLine_Connect : Inherits TaskParent
    Dim knnLine As New KNNLine_Basics
    Public Sub New()
        desc = "Connect each line to its likely predecessor."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        dst3 = dst1.Clone
        labels(3) = labels(2)
        Static lpListLast As New List(Of lpData)(task.lines.lpList)

        knnLine.Run(src)
        dst2 = knnLine.dst2.Clone
        labels(2) = task.lines.labels(2)

        Dim count As Integer
        For i = 0 To task.lines.lpList.Count - 1
            Dim lp1 = task.lines.lpList(i)
            Dim p1 = New cv.Point(dst2.Width, lp1.ptCenter.Y)
            dst2.Line(lp1.ptCenter, p1, task.highlight, task.lineWidth, task.lineType)

            Dim lp2 = lpListLast(lp1.index)
            Dim p2 = New cv.Point2f(0, lp2.ptCenter.Y)
            dst3.Line(p2, lp2.ptCenter, task.highlight, task.lineWidth, task.lineType)
            count += 1
            If count >= 10 Then Exit For
        Next
        dst1 = knnLine.dst2.Clone
        lpListLast = New List(Of lpData)(task.lines.lpList)
    End Sub
End Class





Public Class KNNLine_SliceList : Inherits TaskParent
    Dim knn As New KNN_NNBasics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        OptionParent.FindSlider("KNN Dimension").Value = 1
        desc = "Slice the previous image with a horizontal line at ptCenter's height to " +
               "find all the match candidates"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        Dim knnDimension = knn.options.knnDimension
        dst2.SetTo(0)
        dst3.SetTo(0)
        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Dim lpMatch As lpData
        Static count As Integer
        Static missCount As Integer
        For i = 0 To task.lines.lpList.Count - 1
            Dim lp = task.lines.lpList(i)
            If lp.index > 10 Then Exit For
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
            Dim r = New cv.Rect(0, lp.ptCenter.Y, dst2.Width, 1) ' create a rect for the slice.
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({task.lines.dst1(r)}, {0}, emptyMat, histogram, 1,
                            {task.lines.lpList.Count},
                            New cv.Rangef() {New cv.Rangef(0, task.lines.lpList.Count)})

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            knn.trainInput.Clear()
            knn.queries.Clear()
            For j = 1 To histArray.Count - 1
                If histArray(j) > 0 Then
                    lpMatch = lpListLast(j - 1)
                    'knn.trainInput.Add(lpMatch.p1.X)
                    'knn.trainInput.Add(lpMatch.p1.Y)
                    knn.trainInput.Add(lpMatch.ptCenter.X)
                    'knn.trainInput.Add(lpMatch.ptCenter.Y)
                    'knn.trainInput.Add(lpMatch.p2.X)
                    'knn.trainInput.Add(lpMatch.p2.Y)
                End If
            Next

            'knn.queries.Add(lp.p1.X)
            'knn.queries.Add(lp.p1.Y)
            knn.queries.Add(lp.ptCenter.X)
            'knn.queries.Add(lp.ptCenter.Y)
            'knn.queries.Add(lp.p2.X)
            'knn.queries.Add(lp.p2.Y)

            knn.Run(emptyMat)

            Dim index = Math.Floor(knn.result(0, 0))
            lpMatch = lpListLast(index)
            dst3.Circle(lpMatch.ptCenter, task.DotSize, color, -1)
            If lp.ptCenter.DistanceTo(lpMatch.ptCenter) < 10 Then
                dst3.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
                count += 1
            Else
                missCount += 1
            End If
        Next

        dst1 = task.lines.dst2
        lpListLast = New List(Of lpData)(task.lines.lpList)
        labels(3) = CStr(count) + " lines were confirmed after matching and " + CStr(missCount) +
                    " could not be confirmed since last heartBeatLT"

        If task.heartBeatLT Then
            count = 0
            missCount = 0
        End If
    End Sub
End Class





Public Class KNNLine_SliceTemp : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Slice the previous image with a horizontal line at ptCenter's height to " +
               "find all the match candidates"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        dst2.SetTo(0)
        dst3.SetTo(0)
        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Dim lpMatch As lpData
        Static count As Integer
        Static missCount As Integer
        For i = 0 To task.lines.lpList.Count - 1
            Dim lp = task.lines.lpList(i)
            If lp.index > 10 Then Exit For
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
            Dim r = New cv.Rect(0, lp.ptCenter.Y, dst2.Width, 1) ' create a rect for the slice.
            Dim histogram As New cv.Mat
            cv.Cv2.CalcHist({task.lines.dst1(r)}, {0}, emptyMat, histogram, 1,
                            {task.lines.lpList.Count},
                            New cv.Rangef() {New cv.Rangef(0, task.lines.lpList.Count)})

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            Dim distances As New List(Of Single)
            Dim indexLast As New List(Of Integer)
            For j = 1 To histArray.Count - 1
                If histArray(j) > 0 Then
                    lpMatch = lpListLast(j - 1)
                    'knn.trainInput.Add(lpMatch.p1.X)
                    'knn.trainInput.Add(lpMatch.p1.Y)
                    distances.Add(lp.ptCenter.DistanceTo(lpMatch.ptCenter))
                    indexLast.Add(lpMatch.index)
                    ' knn.trainInput.Add(lpMatch.ptCenter.X)
                    'knn.trainInput.Add(lpMatch.ptCenter.Y)
                    'knn.trainInput.Add(lpMatch.p2.X)
                    'knn.trainInput.Add(lpMatch.p2.Y)
                End If
            Next

            'knn.queries.Add(lp.p1.X)
            'knn.queries.Add(lp.p1.Y)
            'knn.queries.Add(lp.ptCenter.X)
            'knn.queries.Add(lp.ptCenter.Y)
            'knn.queries.Add(lp.p2.X)
            'knn.queries.Add(lp.p2.Y)

            'knn.Run(emptyMat)
            Dim index = indexLast(distances.IndexOf(distances.Min))

            ' Dim index = Math.Floor(knn.result(0, 0))
            lpMatch = lpListLast(index)
            dst3.Circle(lpMatch.ptCenter, task.DotSize, color, -1)
            If lp.ptCenter.DistanceTo(lpMatch.ptCenter) < 10 Then
                dst3.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
                count += 1
            Else
                missCount += 1
            End If
        Next

        dst1 = task.lines.dst2
        lpListLast = New List(Of lpData)(task.lines.lpList)
        labels(3) = CStr(count) + " lines were confirmed after matching and " + CStr(missCount) +
                    " could not be confirmed since last heartBeatLT"

        If task.heartBeatLT Then
            count = 0
            missCount = 0
        End If
    End Sub
End Class






Public Class KNNLine_SliceIndex : Inherits TaskParent
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Compute the distances of the centers of only the longest lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        dst2.SetTo(0)
        dst3.SetTo(0)
        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Dim lpMatch As lpData
        Dim count As Integer
        Dim maxCheck = 10
        For i = 0 To Math.Min(task.lines.lpList.Count, maxCheck) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)

            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
            Dim distances As New List(Of Single)
            Dim indexLast As New List(Of Integer)
            For j = 0 To Math.Min(lpListLast.Count - 1, maxCheck * 2)
                lpMatch = lpListLast(j)
                distances.Add(lp.ptCenter.DistanceTo(lpMatch.ptCenter))
                indexLast.Add(j)
            Next

            Dim index = indexLast(distances.IndexOf(distances.Min))

            lpMatch = lpListLast(index)
            dst3.Circle(lpMatch.ptCenter, task.DotSize, color, -1)
            dst3.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
            count += 1
        Next

        If task.heartBeat Then
            dst1 = task.lines.dst2.Clone
            lpListLast = New List(Of lpData)(task.lines.lpList)
        End If
        If task.heartBeat Then labels(3) = CStr(count) + " lines were matched."
    End Sub
End Class



