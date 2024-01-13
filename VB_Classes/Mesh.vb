Imports cv = OpenCvSharp
Public Class Mesh_Basics : Inherits VB_Algorithm
    Dim knn As New KNN_Basics
    Public Sub New()
        labels(2) = "Triangles built with each random point and its 2 nearest neighbors."
        advice = "Adjust the number of points with the options_random."
        desc = "Build triangles from random points"
    End Sub
    Public Function showMesh(pointList As List(Of cv.Point2f)) As cv.Mat
        knn.queries = pointList
        knn.trainInput = knn.queries
        knn.Run(empty)

        For i = 0 To knn.queries.Count - 1
            Dim p0 = knn.queries(i)
            Dim p1 = knn.queries(knn.result(i, 1))
            dst2.Line(p0, p1, white, task.lineWidth, task.lineType)
            Dim p2 = knn.queries(knn.result(i, 2))
            dst2.Line(p0, p2, white, task.lineWidth, task.lineType)
            dst2.Line(p1, p2, white, task.lineWidth, task.lineType)
        Next
        For i = 0 To knn.queries.Count - 1
            dst2.Circle(knn.queries(i), task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
        Next
        Return dst2
    End Function
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Then
            If standalone Then
                Static random As New Random_Basics
                random.Run(empty)
                dst2.SetTo(0)
                showMesh(random.pointList)
            End If
        End If
    End Sub
End Class






Public Class Mesh_Features : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim mesh As New Mesh_Basics
    Public Sub New()
        labels(2) = "Triangles built with each feature point and its 2 nearest neighbors."
        advice = "Use Options_Features to update results."
        desc = "Build triangles from feature points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        If feat.corners.Count < 3 Then Exit Sub
        mesh.dst2 = src
        dst2 = mesh.showMesh(feat.corners)
    End Sub
End Class






Public Class Mesh_Agast : Inherits VB_Algorithm
    Dim agast As New Feature_Agast
    Dim mesh As New Mesh_Basics
    Public Sub New()
        labels(2) = "Triangles built with each feature point and its 2 nearest neighbors."
        advice = "Use Options_Features to update results."
        desc = "Build triangles from Agast points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        agast.Run(src)
        If agast.stablePoints.Count < 3 Then Exit Sub
        mesh.dst2 = src
        dst2 = mesh.showMesh(agast.stablePoints)
        labels(3) = agast.labels(2)
    End Sub
End Class