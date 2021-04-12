Imports cv = OpenCvSharp
' https://docs.opencv.org/2.4/modules/flann/doc/flann_fast_approximate_nearest_neighbor_search.html#
' https://github.com/JiphuTzu/opencvsharp/blob/master/sample/SamplesVB/Samples/FlannSample.vb
Public Class FLANN_Test
    Inherits VBparent
    Public Sub New()
        initParent()
        task.desc = "Test basics of FLANN - Fast Library for Approximate Nearest Neighbor. "
		' task.rank = 1
        label1 = "FLANN Basics"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        ' creates data set
        Using features As New cv.Mat(10000, 2, cv.MatType.CV_32FC1)
            cv.Cv2.Randu(features, 0, msRNG.Next(9900, 10000))

            ' query
            Dim queryPoint As New cv.Point2f(msRNG.Next(0, 10000), msRNG.Next(0, 10000))
            Dim queries As New cv.Mat(1, 2, cv.MatType.CV_32FC1, queryPoint)

            ' knnSearch
            Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(4))
                Dim knn As Integer = 1
                Dim indices() As Integer = Nothing
                Dim dists() As Single = Nothing
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
                task.trueText(output)
            End Using
        End Using
    End Sub
End Class



' https://docs.opencv.org/3.4/d5/d6f/tutorial_feature_flann_matcher.html
Public Class FLANN_Basics
    Inherits VBparent
    Dim random As Random_Basics
    Dim qArray() As cv.Point2f
    Public Sub New()
        initParent()
        random = New Random_Basics()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Query count", 1, 100, 1)
            sliders.setupTrackBar(1, "Match count", 1, 100, 1)
            sliders.setupTrackBar(2, "Search check count", 1, 1000, 1)
            sliders.setupTrackBar(3, "EPS X100", 0, 100, 0)
        End If
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "Search params sorted"
            check.Box(1).Text = "Reuse the same feature list (test different search parameters)"
            check.Box(1).Checked = True
        End If

        task.desc = "FLANN - Fast Library for Approximate Nearest Neighbor.  Find nearest neighbor"
		' task.rank = 1
        label1 = "Red is query, Nearest points blue"
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim reuseData = check.Box(1).Checked
        If reuseData = False Or task.frameCount = 0 Then random.Run() ' fill result1 with random points in x and y range of the image.
        Dim features As New cv.Mat(random.Points2f.Length, 2, cv.MatType.CV_32F, random.Points2f)

        Dim matchCount = Math.Min(sliders.trackbar(1).Value, random.Points2f.Length - 1)
        Dim queryCount = sliders.trackbar(0).Value
        dst1.SetTo(cv.Scalar.White)
        For i = 0 To features.Rows - 1
            Dim pt = random.Points(i)
            cv.Cv2.Circle(dst1, pt, 5, cv.Scalar.Blue, -1, task.lineType, 0)
        Next

        If reuseData = False Or task.frameCount = 0 Then
            ReDim qArray(sliders.trackbar(0).Value - 1)
            For i = 0 To queryCount - 1
                qArray(i) = New cv.Point2f(msRNG.Next(0, src.Width), msRNG.Next(0, src.Height))
            Next
        End If
        Dim queries As New cv.Mat(queryCount, 2, cv.MatType.CV_32F, qArray)

        Dim searchCheck = sliders.trackbar(2).Value
        Dim eps = sliders.trackbar(3).Value / 100

        Using nnIndex As New cv.Flann.Index(features, New cv.Flann.KDTreeIndexParams(matchCount))
            Dim indices() As Integer = Nothing
            Dim distances() As Single = Nothing
            For i = 0 To queryCount - 1
                Dim pt1 = queries.Get(Of cv.Point2f)(i)
                Dim query As New cv.Mat(1, 2, cv.MatType.CV_32F, pt1)
                nnIndex.KnnSearch(query, indices, distances, matchCount, New cv.Flann.SearchParams(searchCheck, eps, check.Box(0).Checked))

                For j = 0 To matchCount - 1
                    Dim index = indices(j)
                    If index >= 0 And index < random.Points2f.Length Then
                        Dim pt2 = random.Points(index)
                        dst1.Line(pt1, pt2, cv.Scalar.Red, 1, task.lineType)
                    End If
                Next
                cv.Cv2.Circle(dst1, pt1, 5, cv.Scalar.Red, -1, task.lineType, 0)
            Next
        End Using

        Dim output = "FLANN does not appear to be working (perhaps it is my problem) but to show this:" + vbCrLf
        output += "Set query count to 1 and set to reuse the same data - now set as the defaults." + vbCrLf
        output += "The query (in red) is often not picking the nearest blue point." + vbCrLf
        output += "To test further, set the match count to a higher value and observe it will often switch blue dots." + vbCrLf
        output += "Play with the EPS and searchparams check count to see if that helps." + vbCrLf + vbCrLf
        output += "If the 'Search check' is set to 25 and the 'Match count' is set to 4, it does appear to return to the top 4." + vbCrLf
        output += "Perhaps FLANN is only good enough to find a group of neighbors.  Use with caution."
        task.trueText(output, 10, 50, 3)
    End Sub
End Class


