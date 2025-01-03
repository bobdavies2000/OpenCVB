Imports cv = OpenCvSharp
' https://docs.opencvb.org/2.4/modules/flann/doc/flann_fast_approximate_nearest_Neighbors_search.html#
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FlannSample.vb
Public Class FLANN_Test : Inherits TaskParent
    Public Sub New()
        desc = "Test basics of FLANN - Fast Library for Approximate Nearest Neighbor. "
        labels(2) = "FLANN Basics"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        ' creates data set
        Using features As New cv.Mat(10000, 2, cv.MatType.CV_32FC1)
            cv.Cv2.Randu(features, 0, msRNG.Next(9900, 10000))

            ' query
            Dim queryPoint As New cv.Point2f(msRNG.Next(0, 10000), msRNG.Next(0, 10000))
            Dim queries As New cv.Mat(1, 2, cv.MatType.CV_32FC1, queryPoint)

            ' knnSearch
            Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(4))
                Dim knn As Integer = 1
                Dim indices() As Integer
                Dim dists() As Single
                nnIndex.KnnSearch(queries, indices, dists, knn, New cv.Flann.SearchParams(32))

                Dim output = ""
                For i = 0 To knn - 1
                    Dim index As Integer = indices(i)
                    Dim dist As Single = dists(i)
                    Dim pt As New cv.Point2f(features.Get(Of Single)(index, 0), features.Get(Of Single)(index, 1))
                    output += String.Format("No.{0}" & vbTab, i) + vbCrLf
                    output += String.Format("index:{0}", index) + vbCrLf
                    output += String.Format("distance:{0}", dist) + vbCrLf
                    output += String.Format("data:({0}, {1})", pt.X, pt.Y) + vbCrLf
                Next i
                SetTrueText(output)
            End Using
        End Using
    End Sub
End Class









' https://docs.opencvb.org/3.4/d5/d6f/tutorial_feature_flann_matcher.html
Public Class FLANN_Basics : Inherits TaskParent
    Dim random As New Random_Basics
    Dim qArray() As cv.Point2f
    Dim dist As New Distance_Point3D
    Dim options As New Options_FLANN
    Public Sub New()
       optiBase.findslider("Random Pixel Count").Value = 5
        desc = "FLANN - Fast Library for Approximate Nearest Neighbor.  Find nearest neighbor"
        labels(2) = "Red is query, Nearest points blue"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        options.RunOpt()

        If options.reuseData = False Or task.frameCount < 2 Or task.mouseClickFlag Then random.Run(src) ' fill result1 with random points in x and y range of the image.
        Dim features As cv.Mat = cv.Mat.FromPixelData(random.PointList.Count, 2, cv.MatType.CV_32F, random.PointList.ToArray)

        Dim matchCount = Math.Min(options.matchCount, random.PointList.Count - 1)
        dst2.SetTo(white)
        For i = 0 To features.Rows - 1
            Dim pt = random.PointList(i)
            DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Blue)
        Next

        If options.reuseData = False Or task.optionsChanged Or task.mouseClickFlag Then
            ReDim qArray(options.queryCount - 1)
            For i = 0 To options.queryCount - 1
                qArray(i) = New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            Next
        End If
        Dim queries As cv.Mat = cv.Mat.FromPixelData(options.queryCount, 2, cv.MatType.CV_32F, qArray)

        Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(matchCount))
            Dim indices() As Integer
            Dim distances() As Single
            For i = 0 To options.queryCount - 1
                Dim pt1 = queries.Get(Of cv.Point2f)(i)
                Dim query As New cv.Mat(1, 2, cv.MatType.CV_32F, pt1)
                nnIndex.KnnSearch(query, indices, distances, matchCount, New cv.Flann.SearchParams(options.searchCheck, options.eps, options.sorted))

                For j = 0 To matchCount - 1
                    Dim index = indices(j)
                    If index >= 0 And index < random.PointList.Count Then
                        Dim pt2 = random.PointList(index)
                        DrawLine(dst2, pt1, pt2, cv.Scalar.Red)
                    End If
                Next
                DrawCircle(dst2, pt1, task.DotSize, cv.Scalar.Red)
            Next
        End Using

        Dim output = "FLANN does not appear to be working (most likely, it is my problem) but to show this:" + vbCrLf
        output += "Set query count to 1 and set to reuse the same data (defaults.)" + vbCrLf
        output += "The query (in red) is often not picking the nearest blue point." + vbCrLf
        output += "To try different inputs, click anywhere in the image."
        output += "To test further, set the match count to a higher value and observe it will often switch blue dots." + vbCrLf
        output += "Play with the EPS and searchparams check count to see if that helps." + vbCrLf + vbCrLf
        output += "If the 'Search check' is set to 25 and the 'Match count' is set to 4, it does appear to return to the top 4." + vbCrLf
        output += "Perhaps FLANN is only good enough to find a group of neighbors.  Use with caution."
        SetTrueText(output, New cv.Point(10, 50), 3)
    End Sub
End Class