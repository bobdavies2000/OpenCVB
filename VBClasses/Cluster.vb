Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Cluster_Basics : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public ptInput As New List(Of cv.Point)
        Public ptList As New List(Of cv.Point)
        Public clusterID As New List(Of Integer)
        Public clusters As New SortedList(Of Integer, List(Of cv.Point))
        Dim options As New Options_Features
        Dim feat As New Feature_General
        Public Sub New()
            OptionParent.FindSlider("Min Distance").Value = 10
            desc = "Group feature points based on their proximity to each other."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If standalone Then
                feat.Run(src)
                ptInput.Clear()
                For Each pt In feat.ptLatest
                    ptInput.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
                Next
            End If

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

            dst2.SetTo(0)
            For Each group In clusters
                For i = 0 To group.Value.Count - 1
                    For j = 0 To group.Value.Count - 1
                        dst2.Line(group.Value(i), group.Value(j), white, atask.lineWidth, atask.lineWidth)
                    Next
                Next
            Next
            dst3 = src
            For i = 0 To knn.queries.Count - 1
                DrawCircle(dst2, knn.queries(i), atask.DotSize, cv.Scalar.Red)
                DrawCircle(dst3, knn.queries(i), atask.DotSize, atask.highlight)
            Next
            labels(2) = CStr(clusters.Count) + " groups built from " + CStr(ptInput.Count) + " by combining each input point and its nearest neighbor."
            labels(3) = CStr(ptInput.Count) + " input features found."
        End Sub
    End Class






    Public Class NR_Cluster_Hulls : Inherits TaskParent
        Dim cluster As New Cluster_Basics
        Public hulls As New List(Of List(Of cv.Point))
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            desc = "Create hulls for each cluster of feature points found in Cluster_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bPoint.Run(atask.grayStable)

            cluster.ptInput = atask.featurePoints
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
                    dst3.Line(hullPoints(0), hullPoints(1), white, atask.lineWidth, atask.lineWidth)
                Else
                    DrawCircle(dst3, hullPoints(0), atask.DotSize, atask.highlight)
                End If

                hulls.Add(hull)
                If (hull.Count > 0) Then DrawTour(dst3, hull, white, atask.lineWidth)
            Next
            labels(3) = bPoint.labels(2)
        End Sub
    End Class
End Namespace