Imports System.Windows
Imports cvb = OpenCvSharp
Public Class Cluster_Basics : Inherits VB_Parent
    Dim knn As New KNN_Basics
    Public ptInput As New List(Of cvb.Point)
    Public ptList As New List(Of cvb.Point)
    Public clusterID As New List(Of Integer)
    Public clusters As New SortedList(Of Integer, List(Of cvb.Point))
    Dim feat As New Feature_Stable
    Public Sub New()
        FindSlider("Min Distance to next").Value = 10
        desc = "Group the points based on their proximity to each other."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone
        If standalone Then
            feat.Run(src)
            ptInput = task.featurePoints
        End If

        If ptInput.Count <= 3 Then Exit Sub

        knn.queries.Clear()
        For Each pt In ptInput
            knn.queries.Add(New cvb.Point2f(pt.X, pt.Y))
        Next
        knn.trainInput = knn.queries
        knn.Run(empty)

        ptList.Clear()
        clusterID.Clear()
        clusters.Clear()
        Dim groupID As Integer
        For i = 0 To knn.queries.Count - 1
            Dim p1 = New cvb.Point(knn.queries(i).X, knn.queries(i).Y)
            Dim p2 = New cvb.Point(knn.queries(knn.result(i, 1)).X, knn.queries(knn.result(i, 1)).Y)
            Dim index1 = ptList.IndexOf(p1)
            Dim index2 = ptList.IndexOf(p2)
            If index1 >= 0 And index2 >= 0 Then Continue For
            If index1 < 0 And index2 < 0 Then
                ptList.Add(p1)
                ptList.Add(p2)
                groupID = clusters.Count
                Dim newList = New List(Of cvb.Point)({p1, p2})
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
                    DrawLine(dst2, group.Value(i), group.Value(j), cvb.Scalar.White)
                Next
            Next
        Next
        dst3.SetTo(0)
        For i = 0 To knn.queries.Count - 1
            DrawCircle(dst2,knn.queries(i), task.DotSize, cvb.Scalar.Red)
            DrawCircle(dst3,knn.queries(i), task.DotSize, task.HighlightColor)
        Next
        labels(2) = CStr(clusters.Count) + " groups built from " + CStr(ptInput.Count) + " by combining each input point and its nearest neighbor."
    End Sub
End Class






Public Class Cluster_Hulls : Inherits VB_Parent
    Dim cluster As New Cluster_Basics
    Public hulls As New List(Of List(Of cvb.Point))
    Dim feat As New Feature_Stable
    Public Sub New()
        desc = "Create hulls for each cluster of feature points found in Cluster_Basics"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone

        feat.Run(src)
        cluster.ptInput = task.featurePoints
        cluster.Run(src)
        dst2 = cluster.dst2
        dst3 = cluster.dst3

        hulls.Clear()
        For Each group In cluster.clusters
            Dim hullPoints = cvb.Cv2.ConvexHull(group.Value.ToArray, True).ToList
            Dim hull As New List(Of cvb.Point)
            If hullPoints.Count > 2 Then
                For Each pt In hullPoints
                    hull.Add(New cvb.Point(pt.X, pt.Y))
                Next
            ElseIf hullPoints.Count = 2 Then
                DrawLine(dst3, hullPoints(0), hullPoints(1), cvb.Scalar.White)
            Else
                DrawCircle(dst3, hullPoints(0), task.DotSize, task.HighlightColor)
            End If

            hulls.Add(hull)
            If (hull.Count > 0) Then DrawContour(dst3, hull, cvb.Scalar.White, task.lineWidth)
        Next
        labels(3) = feat.labels(3)
    End Sub
End Class






Public Class Cluster_RedCloud : Inherits VB_Parent
    Dim cluster As New Cluster_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "Cluster the center points of the RedCloud cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        cluster.ptInput.Clear()
        Dim smallCellThreshold = src.Total / 1000
        For Each rc In task.redCells
            If rc.pixels < smallCellThreshold And rc.pixels > 0 Then Exit For
            If rc.exactMatch Then cluster.ptInput.Add(rc.maxDist)
        Next
        cluster.Run(src)
        dst3 = cluster.dst2
        If task.heartBeat Then labels(3) = cluster.labels(2)
    End Sub
End Class
