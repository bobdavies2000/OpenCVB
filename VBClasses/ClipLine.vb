Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp : Imports OpenCvSharp.XImgProc
Namespace VBClasses
    Public Class ClipLine_Basics : Inherits TaskParent
        Public rect As cv.Rect ' supply this 
        Public lpList As New List(Of lpData)
        Public Sub New()
            desc = "Find all the line in the specified rect"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                rect = Mat_Basics.buildCenterRect(dst2.Width \ 10, dst2.Height \ 10)
            End If

            lpList.Clear()
            If standaloneTest() Then dst2.SetTo(0)
            Rectangle(dst2, rect, white, task.lineWidth)
            For Each lp In task.lines.lpList
                Dim clipped = ClipLine(rect, lp.p1, lp.p2)
                If clipped Then
                    If standaloneTest() Then Line(dst2, lp.p1, lp.p2, white, task.lineWidth, task.lineType)
                    lpList.Add(lp)
                End If
            Next
        End Sub
    End Class





    Public Class ClipLine_CenterRect : Inherits TaskParent
        Public lpList As New List(Of lpData)
        Public Sub New()
            labels(3) = "Only the lines withing the seelected rectangle"
            dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
            desc = "Use ClipLine to find the lines in the center rect."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = task.lines.dst2
            labels(2) = task.lines.labels(2)

            Dim rect = Mat_Basics.buildCenterRect(dst2.Width \ 10, dst2.Height \ 10)
            lpList.Clear()
            dst3.SetTo(0)
            Rectangle(dst3, rect, white, task.lineWidth)
            For i = 0 To task.lines.lpList.Count - 1
                Dim lp = task.lines.lpList(i)
                Dim clipped = ClipLine(rect, lp.p1, lp.p2)
                If clipped Then
                    Line(dst3, lp.p1, lp.p2, white, task.lineWidth, task.lineType)
                    lpList.Add(lp)
                End If
            Next
        End Sub
    End Class





    Public Class ClipLine_KNN : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public Sub New()
            desc = "Use KNN to connect the each endpoint to its nearest neighbor"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone

            knn.ptListQuery.Clear()
            For Each lp In task.lines.lpList
                knn.ptListQuery.Add(lp.p1)
                knn.ptListQuery.Add(lp.p2)
                Line(dst2, lp.p1, lp.p2, white, task.lineWidth, task.lineType)
                If knn.ptListQuery.Count >= 30 Then Exit For
            Next
            knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)

            knn.Run(emptyMat)

            For i = 0 To knn.result.GetLength(0) - 1
                Dim p1 = knn.ptListTrain(knn.result(i, 0))
                Dim p2 = knn.ptListTrain(knn.result(i, 1))
                Line(dst2, p1, p2, task.highlight, task.lineWidth, task.lineType)
            Next

            Dim rect = Contour_Core.buildRect(knn.ptListQuery.ToArray)
            Rectangle(dst2, rect, white, task.lineWidth, task.lineType)

            labels(2) = CStr(knn.result.GetLength(0)) + " lines were connected to their nearest neighbor"
        End Sub
    End Class





    Public Class ClipLine_Edges : Inherits TaskParent
        Dim knn As New KNN_Basics
        Dim edges As New Edge_EndPoints
        Public Sub New()
            desc = "Use KNN to connect the each endpoint of the LaPlacian output to its nearest neighbor"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(src)

            dst2 = edges.dst3
            labels(2) = edges.labels(3)

            knn.ptListQuery.Clear()
            For Each pt In edges.ptList
                knn.ptListQuery.Add(pt)
            Next
            knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)

            knn.Run(emptyMat)

            dst3.SetTo(0)
            For i = 0 To knn.result.GetLength(0) - 1
                Dim p1 = knn.ptListTrain(knn.result(i, 0))
                Dim p2 = knn.ptListTrain(knn.result(i, 1))
                Line(dst3, p1, p2, task.highlight, task.lineWidth, task.lineType)
            Next

            Dim rect = Contour_Core.buildRect(knn.ptListQuery.ToArray)
            Rectangle(dst2, rect, white, task.lineWidth, task.lineType)

            labels(3) = CStr(knn.result.GetLength(0)) + " lines were connected to their nearest neighbor"
        End Sub
    End Class
End Namespace