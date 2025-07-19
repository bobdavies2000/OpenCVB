Imports OpenCvSharp
Imports cv = OpenCvSharp
Public Class MinMath_Line : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Public lpList As New List(Of lpData) ' lines after being checked with brick points.
    Public Sub New()
        desc = "Track lines with brickpoints."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        Dim linesFound As New List(Of Byte)
        Dim bpList(task.lineRGB.lpList.Count) As List(Of cv.Point)
        For Each bp In bPoints.bpList
            Dim val = task.lineRGB.lpRectMap.Get(Of Byte)(bp.Y, bp.X)
            If val = 0 Then Continue For
            If linesFound.Contains(val) = False Then
                linesFound.Add(val)
                bpList(val) = New List(Of cv.Point)
            End If
            bpList(val).Add(bp)
        Next

        dst3.SetTo(0)
        lpList.Clear()
        For i = 0 To bpList.Count - 1
            If bpList(i) Is Nothing Then Continue For
            Dim p1 = bpList(i)(0)
            Dim p2 = bpList(i)(bpList(i).Count - 1)
            DrawLine(dst2, p1, p2)
            Dim lp = New lpData(p1, p2)
            lpList.Add(lp)
            DrawLine(dst3, p1, p2)
        Next

        For Each index In linesFound
            Dim lp = task.lineRGB.lpList(index - 1)
        Next
        labels(3) = CStr(linesFound.Count) + " lines were confirmed by brick points."
    End Sub
End Class







Public Class MinMath_Edges : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Use brickpoints to track edges."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        edges.Run(src)
        dst3 = edges.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        labels(3) = edges.labels(2)

        For Each bp In bPoints.bpList
            DrawCircle(dst3, bp)
        Next
    End Sub
End Class







Public Class MinMath_EdgeLine : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Public Sub New()
        desc = "Use brickpoints to find edgeLines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        dst3 = task.edges.dst2
        labels(3) = task.edges.labels(2)

        For Each bp In bPoints.bpList
            Dim val = dst3.Get(Of Byte)(bp.Y, bp.X)
            If val = 0 Then Continue For
            DrawCircle(dst3, bp, 255)
        Next
    End Sub
End Class






Public Class MinMath_Neighbors : Inherits TaskParent
    Dim bPoints As New BrickPoint_Basics
    Public Sub New()
        desc = "Connect each brick to its neighbors"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoints.Run(src)
        dst2 = bPoints.dst2
        labels(2) = bPoints.labels(2)

        For Each brick In task.bricks.brickList
        Next
    End Sub
End Class







Public Class MinMath_KNN : Inherits TaskParent
    Dim bPoint As New BrickPoint_Basics
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Connect each brick point with its nearest neighbor"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bPoint.Run(src)
        dst3 = bPoint.dst2
        labels(3) = bPoint.labels(2)

        knn.queries.Clear()
        For Each pt In bPoint.bpList
            knn.queries.Add(New cv.Point2f(pt.X, pt.Y))
        Next

        knn.trainInput = knn.queries
        knn.Run(emptyMat)

        dst2 = src.Clone
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim p2 = knn.queries(knn.neighbors(i)(1))
            DrawLine(dst2, p1, p2)
            Dim p3 = knn.queries(knn.neighbors(i)(2))
            DrawLine(dst3, p1, p2)
        Next
    End Sub
End Class
