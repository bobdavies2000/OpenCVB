Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCVSharp
Public Class XR_MinMath_Edges : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Public Sub New()
        desc = "Use brickpoints to track edges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        CvtColor(task.edges.dst2, dst3, ColorConversionCodes.GRAY2BGR)
        labels(3) = task.edges.labels(2)

        For Each bp In bPoints.ptList
        Circle(dst3, bp, task.DotSize, task.highlight, -1, task.lineType)
        Next
    End Sub
End Class







Public Class XR_MinMath_EdgeLine : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Dim edgeline As New EdgeLine_Basics
    Public Sub New()
        desc = "Use brickpoints to find edgeLines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edgeline.Run(task.gray)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        dst3 = edgeline.dst2
        labels(3) = edgeline.labels(2)

        For Each bp In bPoints.ptList
            Dim val = dst3.Get(Of Byte)(bp.Y, bp.X)
            If val = 0 Then Continue For
            Circle(dst3, bp, task.DotSize, 255, -1, task.lineType)
        Next
    End Sub
End Class





Public Class XR_MinMath_KNN : Inherits TaskParent
    Dim bPoint As New BrickPoint_Basics
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Connect each grid square cv.Point with its nearest neighbor"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(src)
        dst3 = bPoint.dst2
        labels(3) = bPoint.labels(2)

        knn.queries.Clear()
        For Each pt In bPoint.ptList
            knn.queries.Add(New Point2f(pt.X, pt.Y))
        Next

        knn.trainInput = knn.queries
        knn.Run(emptyMat)

        dst2 = src.Clone
        For i = 0 To knn.queries.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.queries(knn.result(i, 1))
            Line(dst2, p1, p2, task.highlight, task.lineWidth, task.lineType)
            Dim p3 = knn.queries(knn.result(i, 2))
            Line(dst3, p1, p2, task.highlight, task.lineWidth, task.lineType)
        Next
    End Sub
End Class

