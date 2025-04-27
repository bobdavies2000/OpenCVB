Imports System.Windows
Imports cv = OpenCvSharp
Public Class Cluster_Basics : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public ptInput As New List(Of cv.Point)
    Public ptList As New List(Of cv.Point)
    Public clusterID As New List(Of Integer)
    Public clusters As New SortedList(Of Integer, List(Of cv.Point))
    Dim options As New Options_Features
    Public Sub New()
        task.featureOptions.DistanceSlider.Value = 10
        desc = "Group the points based on their proximity to each other."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = src.Clone
        If standalone Then ptInput = task.featurePoints

        If ptInput.Count <= 3 Then Exit Sub

        knn.queries.Clear()
        For Each pt In ptInput
            knn.queries.Add(New cv.Point2f(pt.X, pt.Y))
        Next
        knn.trainInput = knn.queries
        knn.Run(src)

        ptList.Clear()
        clusterID.Clear()
        clusters.Clear()
        Dim groupID As Integer
        For i = 0 To knn.queries.Count - 1
            Dim p1 = New cv.Point(knn.queries(i).X, knn.queries(i).Y)
            Dim p2 = New cv.Point(knn.queries(knn.result(i, 1)).X, knn.queries(knn.result(i, 1)).Y)
            Dim index1 = ptList.IndexOf(p1)
            Dim index2 = ptList.IndexOf(p2)
            If index1 >= 0 And index2 >= 0 Then Continue For
            If index1 < 0 And index2 < 0 Then
                ptList.Add(p1)
                ptList.Add(p2)
                groupID = clusters.Count
                Dim newList = New List(Of cv.Point)({p1, p2})
                clusters.Add(groupID, newList)
                clusterID.Add(groupID)
                clusterID.Add(groupID)
            Else
                Dim pt = If(index1 < 0, p1, p2)
                Dim index = If(index1 < 0, index2, index1)
                groupID = clusterID(index)
                ptList.Add(pt)
                clusterID.Add(groupID)
                clusters.ElementAt(groupID).Value.Add(pt)
            End If
        Next

        For Each group In clusters
            For i = 0 To group.Value.Count - 1
                For j = 0 To group.Value.Count - 1
                    DrawLine(dst2, group.Value(i), group.Value(j), white)
                Next
            Next
        Next
        dst3.SetTo(0)
        For i = 0 To knn.queries.Count - 1
            DrawCircle(dst2, knn.queries(i), task.DotSize, cv.Scalar.Red)
            DrawCircle(dst3, knn.queries(i), task.DotSize, task.highlight)
        Next
        labels(2) = CStr(clusters.Count) + " groups built from " + CStr(ptInput.Count) + " by combining each input point and its nearest neighbor."
    End Sub
End Class






Public Class Cluster_Hulls : Inherits TaskParent
    Dim cluster As New Cluster_Basics
    Public hulls As New List(Of List(Of cv.Point))
    Dim gridPoint As New GridPoint_Basics
    Public Sub New()
        desc = "Create hulls for each cluster of feature points found in Cluster_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        gridPoint.Run(task.grayStable)

        cluster.ptInput = task.featurePoints
        cluster.Run(src)
        dst2 = cluster.dst2
        dst3 = cluster.dst3

        hulls.Clear()
        For Each group In cluster.clusters
            Dim hullPoints = cv.Cv2.ConvexHull(group.Value.ToArray, True).ToList
            Dim hull As New List(Of cv.Point)
            If hullPoints.Count > 2 Then
                For Each pt In hullPoints
                    hull.Add(New cv.Point(pt.X, pt.Y))
                Next
            ElseIf hullPoints.Count = 2 Then
                DrawLine(dst3, hullPoints(0), hullPoints(1), white)
            Else
                DrawCircle(dst3, hullPoints(0), task.DotSize, task.highlight)
            End If

            hulls.Add(hull)
            If (hull.Count > 0) Then DrawContour(dst3, hull, white, task.lineWidth)
        Next
        labels(3) = gridPoint.labels(2)
    End Sub
End Class
