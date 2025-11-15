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
        Dim lpListLast As New List(Of lpData)(task.lines.lpList)

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
    End Sub
End Class





Public Class KNNLine_IDLines : Inherits TaskParent
    Dim knnLine As New KNNLine_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Use CalcHist to find matching lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count = 0 Then Exit Sub ' nothing to work on yet.

        dst1.SetTo(0)
        For Each lp In task.lines.lpList
            dst1.Circle(lp.ptCenter, 5, 255, -1)
        Next

        Dim histogram As New cv.Mat
        Dim histarray(task.lines.lpList.Count - 1) As Single
        Dim ranges1 = New cv.Rangef() {New cv.Rangef(0, 255)}
        cv.Cv2.CalcHist({task.lines.dst1}, {0}, dst1, histogram, 1, {task.lines.lpList.Count}, ranges1)
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)
    End Sub
End Class
