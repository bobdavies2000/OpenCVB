Imports cv = OpenCvSharp
Public Class KNN_Basics : Inherits VBparent
    Public neighbors As New cv.Mat
    Public desiredMatches = 1
    Public knn As cv.ML.KNearest
    Public lastSet As New List(Of cv.Point2f)
    Public currSet As New List(Of cv.Point2f)
    Dim random As New Random_Basics
    Public Sub New()
        label1 = "White=TrainingData, Red=queries"
        knn = cv.ML.KNearest.Create()
        task.desc = "Test knn with random points in the image.  Find the nearest n points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1.SetTo(cv.Scalar.Black)

        If standalone Then
            random.Run(Nothing)
            lastSet = New List(Of cv.Point2f)(random.Points2f)
            random.Run(Nothing)
            currSet = New List(Of cv.Point2f)(random.Points2f)
        End If

        Dim queries = New cv.Mat(currSet.Count, 2, cv.MatType.CV_32F, currSet.ToArray)
        Dim trainData = New cv.Mat(lastSet.Count, 2, cv.MatType.CV_32F, lastSet.ToArray)

        Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S)
        For i = 0 To trainData.Rows - 1
            response.Set(Of Integer)(i, 0, i)
            cv.Cv2.Circle(dst1, trainData.Get(Of cv.Point2f)(i, 0), 5, cv.Scalar.White, -1, task.lineType, 0)
        Next
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
        knn.FindNearest(queries, desiredMatches, New cv.Mat, neighbors)

        If standalone Then
            For i = 0 To neighbors.Rows - 1
                Dim qPoint = queries.Get(Of cv.Point2f)(i, 0)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, task.lineType, 0)
                Dim pt = trainData.Get(Of cv.Point2f)(neighbors.Get(Of Single)(i, 0), 0)
                dst1.Line(pt, qPoint, cv.Scalar.Red, 1, task.lineType)
            Next
        End If
    End Sub
End Class







Public Class KNN_BasicsQT : Inherits VBparent
    Public neighbors As New cv.Mat
    Public testMode As Boolean
    Public desiredMatches = 1
    Public knn As cv.ML.KNearest
    Public knnQT As New KNN_Options
    Public Sub New()
        If standalone Then knnQT.useRandomData = True

        label1 = "White=TrainingData, Red=queries"
        knn = cv.ML.KNearest.Create()
        task.desc = "Test knn with random points in the image.  Find the nearest n points."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1.SetTo(cv.Scalar.Black)

        If standalone Or knnQT.useRandomData Then
            knnQT.Run(src)
            knnQT.trainingPoints = New List(Of cv.Point2f)(knnQT.randomTrain.Points2f)
            knnQT.queryPoints = New List(Of cv.Point2f)(knnQT.randomQuery.Points2f)
        Else
            If knnQT.queryPoints.Count = 0 Then Exit Sub ' nothing to do on this generation...
        End If
        ' The first generation may not have any training data, only queries.  (Queries move to training on subsequent generations.)
        If knnQT.trainingPoints.Count = 0 Then knnQT.trainingPoints = New List(Of cv.Point2f)(knnQT.queryPoints)

        Dim queries = New cv.Mat(knnQT.queryPoints.Count, 2, cv.MatType.CV_32F, knnQT.queryPoints.ToArray)
        Dim trainData = New cv.Mat(knnQT.trainingPoints.Count, 2, cv.MatType.CV_32F, knnQT.trainingPoints.ToArray)

        Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S)
        For i = 0 To trainData.Rows - 1
            response.Set(Of Integer)(i, 0, i)
            cv.Cv2.Circle(dst1, trainData.Get(Of cv.Point2f)(i, 0), 5, cv.Scalar.White, -1, task.lineType, 0)
        Next
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
        knn.FindNearest(queries, desiredMatches, New cv.Mat, neighbors)

        If standalone Or testMode Then
            For i = 0 To neighbors.Rows - 1
                Dim qPoint = queries.Get(Of cv.Point2f)(i, 0)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, task.lineType, 0)
                Dim pt = trainData.Get(Of cv.Point2f)(neighbors.Get(Of Single)(i, 0), 0)
                dst1.Line(pt, qPoint, cv.Scalar.Red, 1, task.lineType)
            Next
        End If
    End Sub
End Class






Public Class KNN_Options : Inherits VBparent
    Public trainingPoints As New List(Of cv.Point2f)
    Public queryPoints As New List(Of cv.Point2f)
    Public randomTrain As New Random_Basics
    Public randomQuery As New Random_Basics
    Public useRandomData As Boolean
    Public Sub New()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "KNN Query count", 1, 100, 10)
            sliders.setupTrackBar(1, "KNN Train count", 1, 100, 20)
            sliders.setupTrackBar(2, "KNN k nearest points", 1, 100, 1)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Reuse the training and query data"
        End If

        label1 = "Random training points"
        label2 = "Random query points"
        task.desc = "Source of query/train points - generate points if standalone.  Reuse points if requested."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static trainSlider = findSlider("KNN Train count")
        Static querySlider = findSlider("KNN Query count")
        If standalone Or task.intermediateReview = caller Then
            If check.Box(0).Checked = False Then useRandomData = True
        End If

        If useRandomData Then
            randomTrain.countSlider.Value = trainSlider.Value
            randomTrain.Run(Nothing)

            randomQuery.countSlider.Value = querySlider.Value
            randomQuery.Run(Nothing)
        End If

        ' algorithm does nothing but provide a location for query/train points when not running standalone.
        If standalone Or task.intermediateReview = caller Then
            ' query/train points need to be manufactured when standalone
            trainingPoints = New List(Of cv.Point2f)(randomTrain.Points2f)
            queryPoints = New List(Of cv.Point2f)(randomQuery.Points2f)

            dst1.SetTo(cv.Scalar.White)
            dst2.SetTo(cv.Scalar.White)
            For i = 0 To randomTrain.Points2f.Count - 1
                Dim pt = randomTrain.Points2f(i)
                cv.Cv2.Circle(dst1, pt, 5, cv.Scalar.Blue, -1, task.lineType, 0)
            Next
            For i = 0 To randomQuery.Points2f.Count - 1
                Dim pt = randomQuery.Points2f(i)
                cv.Cv2.Circle(dst2, pt, 5, cv.Scalar.Red, -1, task.lineType, 0)
            Next
        End If
    End Sub
End Class






Public Class KNN_1_to_1 : Inherits VBparent
    Public matchedPoints() As cv.Point2f
    Public unmatchedPoints As New List(Of cv.Point2f)
    Public basics As New KNN_BasicsQT
    Public Sub New()
        If standalone Then basics.knnQT.useRandomData = True
        basics.desiredMatches = 4 ' more than 1 to insure there are secondary choices below for 1:1 matching below.

        label1 = "White=TrainingData, Red=queries, yellow=unmatched"
        task.desc = "Use knn to find the nearest n points but use only the best and no duplicates - 1:1 mapping."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        basics.Run(src)
        dst1 = basics.dst1

        ReDim matchedPoints(basics.knnQT.queryPoints.Count - 1)
        Dim neighborOffset(basics.knnQT.queryPoints.Count - 1) As Integer
        For i = 0 To matchedPoints.Count - 1
            matchedPoints(i) = basics.knnQT.trainingPoints(basics.neighbors.Get(Of Single)(i, 0))
        Next

        ' map the points 1 to 1: find duplicate best fits, choose which is better.
        ' loser must relinquish the training data element And use its next neighbor
        Dim changedNeighbors As Boolean = True
        While changedNeighbors
            changedNeighbors = False
            For i = 0 To matchedPoints.Count - 1
                Dim m1 = matchedPoints(i)
                For j = i + 1 To matchedPoints.Count - 1
                    Dim m2 = matchedPoints(j)
                    If m1.X = -1 Or m2.X = -1 Then Continue For
                    If m1 = m2 Then
                        changedNeighbors = True
                        Dim pt1 = basics.knnQT.queryPoints(i)
                        Dim pt2 = basics.knnQT.queryPoints(j)
                        Dim distance1 = Math.Sqrt((pt1.X - m1.X) * (pt1.X - m1.X) + (pt1.Y - m1.Y) * (pt1.Y - m1.Y))
                        Dim distance2 = Math.Sqrt((pt2.X - m1.X) * (pt2.X - m1.X) + (pt2.Y - m1.Y) * (pt2.Y - m1.Y))
                        Dim ij = If(distance1 > distance2, i, j)
                        Dim unresolved = True
                        If ij < neighborOffset.Length Then
                            If neighborOffset(ij) < basics.neighbors.Rows - 1 Then
                                neighborOffset(ij) += 1
                                Dim index = basics.neighbors.Get(Of Single)(neighborOffset(ij))
                                If index < basics.knnQT.trainingPoints.Count And index >= 0 Then
                                    unresolved = False
                                    matchedPoints(ij) = basics.knnQT.trainingPoints(index)
                                End If
                            End If
                        End If
                        If unresolved Then
                            matchedPoints(ij) = New cv.Point2f(-1, -1)
                            Exit For
                        End If
                    End If
                Next
            Next
        End While

        unmatchedPoints.Clear()
        For i = 0 To matchedPoints.Count - 1
            Dim mpt = matchedPoints(i)
            Dim qPoint = basics.knnQT.queryPoints(i)
            If mpt.X >= 0 Then
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Red, -1, task.lineType, 0)
                dst1.Line(mpt, qPoint, cv.Scalar.Red, 1, task.lineType)
            Else
                unmatchedPoints.Add(qPoint)
                cv.Cv2.Circle(dst1, qPoint, 3, cv.Scalar.Yellow, -1, task.lineType, 0)
            End If
        Next
    End Sub
End Class






Public Class KNN_Emax : Inherits VBparent
    Public knn As New KNN_1_to_1
    Dim emax As New EMax_Centroids
    Public Sub New()
        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Map queries to training data 1:1 (Off means many:1)"
            check.Box(1).Text = "Display queries"
            check.Box(2).Text = "Display training input and connecting line"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
            check.Box(2).Checked = True
        End If

        knn.basics.knnQT.useRandomData = False

        label1 = "Output from Emax"
        label2 = "White=TrainingData, Red=queries yellow=unmatched"
        task.desc = "Emax centroids move but here KNN is used to matched the old and new locations and keep the colors the same."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If standalone And task.frameCount = 0 Then
            emax = New EMax_Centroids()
            emax.Run(src) ' set the first generation of points.
        End If
        If standalone Or task.intermediateReview = caller Then
            knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(emax.flood.centroids)
            emax.Run(src)
            knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(emax.flood.centroids)
        End If

        knn.Run(src)
        If standalone Or task.intermediateReview = caller Then
            dst1 = emax.dst1 + knn.dst1
            dst2 = knn.dst1
        Else
            dst1 = knn.dst1
        End If
    End Sub
End Class






Public Class KNN_Test : Inherits VBparent
    Public grid As New Thread_Grid
    Dim knn As New KNN_BasicsQT
    Public Sub New()
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Minimum = 50 ' limit the number of centroids - KNN can't handle more than a few thousand without rework.
        gridHeightSlider.Minimum = 50
        gridWidthSlider.Value = 100
        gridHeightSlider.Value = 100

        knn.testMode = True

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Show grid mask"
        End If

        task.desc = "Assign random values inside a thread grid to test that KNN is properly tracking them."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        knn.knnQT.queryPoints.Clear()
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList.ElementAt(i)
            Dim pt = New cv.Point2f(roi.X + msRNG.Next(roi.Width), roi.Y + msRNG.Next(roi.Height))
            knn.knnQT.queryPoints.Add(pt)
        Next

        knn.Run(src)
        dst1 = knn.dst1
        knn.knnQT.trainingPoints = New List(Of cv.Point2f)(knn.knnQT.queryPoints)
        label1 = knn.label1
        If check.Box(0).Checked Then dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class





Public Class KNN_Test_1_to_1 : Inherits VBparent
    Public grid As New Thread_Grid
    Dim knn As New KNN_1_to_1
    Public Sub New()
        Dim gridWidthSlider = findSlider("ThreadGrid Width")
        Dim gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Minimum = 50 ' limit the number of centroids - KNN can't handle more than a few thousand without rework.
        gridHeightSlider.Minimum = 50
        gridWidthSlider.Value = 100
        gridHeightSlider.Value = 100

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 1)
            check.Box(0).Text = "Show grid mask"
        End If
        task.desc = "Assign random values inside a thread grid to test that KNN is properly tracking them."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        knn.basics.knnQT.queryPoints.Clear()
        For i = 0 To grid.roiList.Count - 1
            Dim roi = grid.roiList.ElementAt(i)
            Dim pt = New cv.Point2f(roi.X + msRNG.Next(roi.Width), roi.Y + msRNG.Next(roi.Height))
            knn.basics.knnQT.queryPoints.Add(pt)
        Next

        knn.Run(src)
        dst1 = knn.dst1
        knn.basics.knnQT.trainingPoints = New List(Of cv.Point2f)(knn.basics.knnQT.queryPoints)
        label1 = knn.label1
        If check.Box(0).Checked Then dst1.SetTo(cv.Scalar.White, grid.gridMask)
    End Sub
End Class






Public Class KNN_Point3d : Inherits VBparent
    Public querySet() As cv.Point3f
    Public kOptions As New KNN_Options
    Public responseSet() As Integer
    Public lastSet() As cv.Point3f ' default usage: find and connect points in 2D for this number of points.
    Public Sub New()
        task.desc = "Use KNN to connect 3D points.  Results shown are a 2D projection of the 3D results."
        label1 = "Yellow=Query (in 3D) Blue=Best Response (in 3D)"
        label2 = "Top Down View to confirm 3D KNN is correct"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static countSlider = findSlider("KNN Query count")
        Static nearestCountSlider = findSlider("KNN k nearest points")
        Dim findXnearest = nearestCountSlider.value

        Dim maxDepth As Integer = 4000 ' this is an arbitrary max depth
        Dim knn = cv.ML.KNearest.Create()
        If standalone Or task.intermediateReview = caller Then
            ReDim lastSet(countSlider.Value - 1)
            ReDim querySet(lastSet.Count - 1)
            For i = 0 To lastSet.Count - 1
                lastSet(i) = New cv.Point3f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows), msRNG.Next(0, maxDepth))
            Next

            For i = 0 To querySet.Count - 1
                querySet(i) = New cv.Point3f(msRNG.Next(0, dst1.Cols), msRNG.Next(0, dst1.Rows), msRNG.Next(0, maxDepth))
            Next
        End If
        Dim responses(lastSet.Length - 1) As Integer
        For i = 0 To responses.Length - 1
            responses(i) = i
        Next

        Dim trainData = New cv.Mat(lastSet.Length, 2, cv.MatType.CV_32F, lastSet)
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, New cv.Mat(responses.Length, 1, cv.MatType.CV_32S, responses))

        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 0 To lastSet.Count - 1
            Dim p = New cv.Point2f(lastSet(i).X, lastSet(i).Y)
            dst1.Circle(p, 9, cv.Scalar.Blue, -1, task.lineType)
            p = New cv.Point2f(lastSet(i).X, lastSet(i).Z * src.Rows / maxDepth)
            dst2.Circle(p, 9, cv.Scalar.Blue, -1, task.lineType)
        Next

        ReDim responseSet(querySet.Length * findXnearest - 1)
        For i = 0 To querySet.Count - 1
            query.Set(Of cv.Point3f)(0, 0, querySet(i))
            knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To findXnearest - 1
                responseSet(i * findXnearest + j) = CInt(neighbors.Get(Of Single)(0, j))
            Next
            If standalone Or task.intermediateReview = caller Then
                For j = 0 To findXnearest - 1
                    Dim plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Y)
                    Dim pQ = New cv.Point2f(querySet(i).X, querySet(i).Y)
                    dst1.Line(plast, pQ, cv.Scalar.White, 1, task.lineType)
                    dst1.Circle(pQ, 5, cv.Scalar.Yellow, -1, task.lineType, 0)

                    plast = New cv.Point2f(lastSet(responseSet(i * findXnearest + j)).X, lastSet(responseSet(i * findXnearest + j)).Z * src.Rows / maxDepth)
                    pQ = New cv.Point2f(querySet(i).X, querySet(i).Z * src.Rows / maxDepth)
                    dst2.Line(plast, pQ, cv.Scalar.White, 1, task.lineType)
                    dst2.Circle(pQ, 5, cv.Scalar.Yellow, -1, task.lineType, 0)
                Next
            End If
        Next
    End Sub
End Class







Public Class KNN_DepthClusters : Inherits VBparent
    Public blobs As New Blob_DepthClusters
    Public flood As New FloodFill_Palette
    Public pTrack As New KNN_PointTracker
    Public Sub New()
        label1 = "Output of Blob_DepthClusters"
        label2 = "Same output after KNN_PointTracker"
        task.desc = "Use KNN to track and color the Blob results from clustering the depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        blobs.Run(src)
        dst1 = blobs.dst2

        flood.Run(dst1)

        pTrack.queryPoints = flood.basics.centroids
        pTrack.queryMasks = flood.basics.masks
        pTrack.queryRects = flood.basics.rects
        pTrack.Run(src)
        dst2 = pTrack.dst1
    End Sub
End Class






Public Class KNN_SmoothAverage : Inherits VBparent
    Dim knn As New KNN_DepthClusters
    Dim lastinput As New cv.Mat
    Public Sub New()
        findCheckBox("Draw rectangle and centroid for each mask").Checked = False

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Weight X100", 0, 100, 50)
        End If

        label1 = "AddWeight result of current and previous frame"
        label2 = "Mask for difference between current and last frame"
        task.desc = "Smooth out the abrupt appearance/disappearance of floodfilled regions"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        knn.Run(src)

        Static accum As New cv.Mat
        If task.frameCount = 0 Then accum = knn.dst2.Clone

        Dim alpha = sliders.trackbar(0).Value / 100
        cv.Cv2.AddWeighted(knn.dst2, alpha, accum, 1.0 - alpha, 0, accum)
        dst1 = accum

        Dim tmp = knn.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
        If task.frameCount = 0 Then lastinput = tmp.Clone
        cv.Cv2.BitwiseXor(tmp, lastinput, dst2)
        lastinput = tmp
    End Sub
End Class






Public Class KNN_StabilizeRegions : Inherits VBparent
    Public knn As New KNN_DepthClusters
    Public flood As New FloodFill_Palette
    Dim lastinput As New cv.Mat
    Public Sub New()
        findCheckBox("Draw rectangle and centroid for each mask").Checked = False
        label1 = "Output of KNN_DepthClusters"
        label2 = "KNN_DepthClusters output plus unstable regions"
        task.desc = "Identify major regions that are unstable - appearing and disappearing"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        knn.Run(src)
        dst1 = knn.dst2

        Dim tmp = knn.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY).ConvertScaleAbs(255)
        If task.frameCount = 0 Then lastinput = tmp.Clone
        cv.Cv2.BitwiseXor(tmp, lastinput, dst2)
        lastinput = tmp

        flood.Run(dst2)
        dst2 = flood.dst2

    End Sub
End Class






Public Class KNN_Contours : Inherits VBparent
    Dim outline As New Contours_Depth
    Dim knn As New KNN_BasicsQT
    Public Sub New()
        task.desc = "Use KNN to streamline the outline of a contour"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        outline.Run(src)
        dst1 = outline.dst2

        knn.knnQT.trainingPoints.Clear()
        If task.frameCount = 0 Then
            For i = 0 To outline.contours.Count - 1
                knn.knnQT.trainingPoints.Add(New cv.Point2f(outline.contours(i).X, outline.contours(i).Y))
            Next
        Else
            knn.knnQT.queryPoints.Clear()
            For i = 0 To outline.contours.Count - 1
                knn.knnQT.queryPoints.Add(New cv.Point2f(outline.contours(i).X, outline.contours(i).Y))
            Next
        End If

        knn.Run(src)

        Dim queries = New cv.Mat(knn.knnQT.queryPoints.Count, 2, cv.MatType.CV_32F, knn.knnQT.queryPoints.ToArray)
        Dim trainData = New cv.Mat(knn.knnQT.trainingPoints.Count, 2, cv.MatType.CV_32F, knn.knnQT.trainingPoints.ToArray)
        dst2.SetTo(0)
        For i = 0 To knn.neighbors.Rows - 1
            Dim qPoint = queries.Get(Of cv.Point2f)(i, 0)
            cv.Cv2.Circle(dst2, qPoint, 3, cv.Scalar.Red, -1, task.lineType, 0)
            Dim pt = trainData.Get(Of cv.Point2f)(knn.neighbors.Get(Of Single)(i, 0), 0)
            dst2.Line(pt, qPoint, cv.Scalar.Red, 1, task.lineType)
        Next
    End Sub
End Class








Public Class KNN_Cluster2DCities : Inherits VBparent
    Dim knn As New KNN_Point2d
    Public cityPositions() As cv.Point
    Public cityOrder() As Integer
    Public distances() As Integer
    Dim numberOfCities As Integer
    Dim closedRegions As Integer
    Dim nearestCountSlider As Windows.Forms.TrackBar
    Dim cityCountSlider As Windows.Forms.TrackBar
    Public Sub New()
        ' If they changed Then number of elements in the set
        cityCountSlider = findSlider("KNN Query count")
        nearestCountSlider = findSlider("KNN k nearest points")
        nearestCountSlider.Value = cityCountSlider.Value

        label1 = ""
        task.desc = "Use knn to cluster cities - a primitive attempt at traveling salesman problem."
    End Sub
    Public Sub cluster(result As cv.Mat, findXnearest As Integer)
        Dim alreadyTaken As New List(Of Integer)
        For i = 0 To numberOfCities - 1
            For j = 1 To numberOfCities - 1
                Dim nearestCity = knn.responseSet(i * findXnearest + j)
                ' the last entry will never have a city to connect to so just connect with the nearest.
                If i = numberOfCities - 1 Then
                    cityOrder(i) = nearestCity
                    Exit For
                End If
                If alreadyTaken.Contains(nearestCity) = False Then
                    cityOrder(i) = nearestCity
                    alreadyTaken.Add(nearestCity)
                    Exit For
                End If
            Next
        Next
        For i = 0 To cityOrder.Length - 1
            result.Line(cityPositions(i), cityPositions(cityOrder(i)), cv.Scalar.White, 4 * task.fontSize)
        Next

        closedRegions = 0
        For y = 0 To result.Rows - 1
            For x = 0 To result.Cols - 1
                If result.Get(Of cv.Vec3b)(y, x) = cv.Scalar.Black Then
                    Dim byteCount = cv.Cv2.FloodFill(result, New cv.Point(x, y), task.vecColors(closedRegions Mod task.vecColors.Length))
                    If byteCount > 10 Then closedRegions += 1 ' there are fake regions due to anti-alias like features that appear when drawing.
                End If
            Next
        Next
        For i = 0 To cityOrder.Length - 1
            result.Circle(cityPositions(i), 4 * task.fontSize, cv.Scalar.Red, -1, task.lineType)
        Next
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static reuseCheck = findCheckBox("Reuse the training and query data")

        If cityCountSlider.Value <> numberOfCities Or reuseCheck.Checked = False Then
            numberOfCities = cityCountSlider.Value

            ReDim cityPositions(numberOfCities - 1)
            ReDim cityOrder(numberOfCities - 1)

            Dim gen As New System.Random()
            Dim r As New cv.RNG(gen.Next(0, 1000000))
            For i = 0 To numberOfCities - 1
                cityPositions(i).X = r.Uniform(0, src.Width)
                cityPositions(i).Y = r.Uniform(0, src.Height)
            Next

            ' find the nearest neighbor for each city - first will be the current city, next will be nearest real neighbors in order
            knn.knn.knnQT.trainingPoints.Clear()
            knn.knn.knnQT.queryPoints.Clear()
            For i = 0 To numberOfCities - 1
                knn.knn.knnQT.trainingPoints.Add(New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y)))
                knn.knn.knnQT.queryPoints.Add(New cv.Point2f(CSng(cityPositions(i).X), CSng(cityPositions(i).Y)))
            Next
            knn.Run(src)

            dst1.SetTo(0)
            cluster(dst1, nearestCountSlider.Value)
            task.trueText("knn closed regions = " + CStr(closedRegions), 10, 40, 3)
        End If
    End Sub
End Class









Public Class KNN_Point2d : Inherits VBparent
    Public knn As New KNN_BasicsQT
    Public responseSet() As Integer
    Public Sub New()
        If standalone Then knn.knnQT.useRandomData = True

        task.desc = "Use KNN to find n matching points for each query."
        label1 = "Yellow=Queries, Blue=Best Responses"
    End Sub
    Public Sub prepareImage(dst As cv.Mat, dotSize As Integer)
        dst.SetTo(0)
        'For i = 0 To knn.knnQT.trainingPoints.Count - 1
        '    cv.Cv2.Circle(dst, knn.knnQT.trainingPoints(i), dotSize + 2, cv.Scalar.Blue, -1, task.lineType, 0)
        'Next
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static nearestCountSlider = findSlider("KNN k nearest points")
        Dim findXnearest = nearestCountSlider.Value

        If standalone Or task.intermediateReview = caller Then prepareImage(dst1, task.dotSize)

        knn.Run(src)

        ReDim responseSet(knn.knnQT.queryPoints.Count * findXnearest - 1)
        Dim results As New cv.Mat, neighbors As New cv.Mat, query As New cv.Mat(1, 2, cv.MatType.CV_32F)
        For i = 0 To knn.knnQT.queryPoints.Count - 1
            query.Set(Of cv.Point2f)(0, 0, knn.knnQT.queryPoints(i))
            knn.knn.FindNearest(query, findXnearest, results, neighbors)
            For j = 0 To neighbors.Cols - 1
                Dim index = neighbors.Get(Of Single)(0, j)
                responseSet(i * findXnearest + j) = CInt(index)
            Next
            If standalone Or task.intermediateReview = caller Then
                For j = 0 To findXnearest - 1
                    dst1.Line(knn.knnQT.trainingPoints(responseSet(i * findXnearest + j)), knn.knnQT.queryPoints(i), cv.Scalar.White, 1, task.lineType)
                    cv.Cv2.Circle(dst1, knn.knnQT.trainingPoints(responseSet(i * findXnearest + j)), task.dotSize, cv.Scalar.Blue, -1, task.lineType, 0)
                    cv.Cv2.Circle(dst1, knn.knnQT.queryPoints(i), task.dotSize, cv.Scalar.Yellow, -1, task.lineType, 0)
                Next
            End If
        Next
    End Sub
End Class







Public Class KNN_Learn : Inherits VBparent
    Public trainingPoints As New List(Of cv.Point2f)
    Public knn As cv.ML.KNearest
    Public Sub New()
        knn = cv.ML.KNearest.Create()
        task.desc = "Learn from a set of training points.  The calling user can then use FindNearest on the knn"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1.SetTo(cv.Scalar.Black)

        If standalone Then
            task.trueText("Database is ready for queries.  Use it with code like this " + vbCrLf + vbCrLf +
                          "public learn as New KNN_Learn" + vbCrLf + "learn = new KNN_Learn" + vbCrLf +
                          "Dim neighbors As New cv.Mat" + vbCrLf + "Dim queries = New cv.Mat(1, 2, cv.MatType.CV_32F, {pt.x, pt.y})" + vbCrLf +
                          "learn.knn.FindNearest(queries, 1, neighbors)" + vbCrLf + vbCrLf + "And neighbors will have nearest point." + vbCrLf +
                          "See KNN_Learn for code to cut and paste...")

            ' cut and paste this code into any new algorithm to use KNN_Learn
            '''''''''' Dim learn as New KNN_Learn
            '''''''''' learn = New KNN_Learn
            '''''''''' Dim neighbors As New cv.Mat
            '''''''''' Dim queries = New cv.Mat(1, 2, cv.MatType.CV_32F, {)
            '''''''''' knn.run()
            '''''''''' learn.knn.FindNearest(queries, 1, neighbors)
        End If

        If trainingPoints.Count = 0 Then Exit Sub
        Dim trainData = New cv.Mat(trainingPoints.Count, 2, cv.MatType.CV_32F, trainingPoints.ToArray)

        Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S)
        For i = 0 To trainData.Rows - 1
            response.Set(Of Integer)(i, 0, i)
        Next
        knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
    End Sub
End Class





Public Structure viewObject
    Dim centroid As cv.Point2f
    Dim preKalmanRect As cv.Rect
    Dim rectFront As cv.Rect ' this becomes the front view after processing.
    Dim rectInHist As cv.Rect ' this rectangle describe the object in the histogram (side or top view.)
    Dim LayoutColor As cv.Scalar
    Dim floodPoint As cv.Point
    Dim mask As cv.Mat
End Structure





Public Class KNN_PointTracker : Inherits VBparent
    Public knn As New KNN_1_to_1
    Dim newCentroids As New List(Of cv.Point2f)
    Public kalman As New List(Of Kalman_Stripped)
    Public kalmanOptions As New Kalman_Basics
    Public queryPoints As New List(Of cv.Point2f)
    Public queryRects As New List(Of cv.Rect)
    Public queryMasks As New List(Of cv.Mat)
    Public floodPoints As New List(Of cv.Point)
    Public drawRC As New Draw_ViewObjects
    Public Sub New()
        allocateKalman(16) ' allocate some kalman objects

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Minimum size of object in pixels", 1, 10000, 3000)
        End If

        task.desc = "Use KNN to track points and Kalman to smooth the results"
    End Sub
    Private Sub allocateKalman(count As Integer)
        For i = kalman.Count To count - 1
            kalman.Add(New Kalman_Stripped())
            ReDim kalman(i).kInput(6 - 1)
            If i < queryPoints.Count Then
                kalman(i).kInput = New Single() {queryPoints(i).X, queryPoints(i).Y, 0, 0, 0, 0}
            Else
                kalman(i).kInput = New Single() {-1, -1, 0, 0, 0, 0}
                kalman(i).kOutput = New Single() {-1, -1, 0, 0, 0, 0}
            End If
        Next
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static pixelSlider = findSlider("Minimum size of object in pixels")
        Static drawRCCheck = findCheckBox("Caller will handle any drawing required")
        If standalone Or task.intermediateReview = caller Then
            task.trueText("KNN_PointTracker running standalone has no output.")
            Exit Sub
        End If

        ' allocate the kalman filters for each centroid with some additional filters for objects that come and go...
        If kalman.Count < queryPoints.Count + newCentroids.Count Then allocateKalman(queryPoints.Count + newCentroids.Count)

        knn.basics.knnQT.trainingPoints.Clear()
        ' The previous generation's query points becomes the trainingpoints for the current generation here.
        For i = 0 To kalman.Count - 1
            If kalman(i).kInput(0) >= 0 Then knn.basics.knnQT.trainingPoints.Add(New cv.Point2f(kalman(i).kInput(0), kalman(i).kInput(1)))
        Next

        If newCentroids.Count > 0 Then
            ' when the queries outnumber the trainingpoints, some new queries need to be added.
            Dim qIndex As Integer
            For i = knn.basics.knnQT.trainingPoints.Count To kalman.Count - 1
                If qIndex >= newCentroids.Count Then Exit For
                knn.basics.knnQT.trainingPoints.Add(newCentroids(qIndex))
                kalman(i).kInput = {newCentroids(qIndex).X, newCentroids(qIndex).Y, 0, 0, 0, 0}
                qIndex += 1
                If qIndex >= kalman.Count Then Exit Sub ' we don't have enough kalman filters to handle this level of queries so restart
            Next
        End If
        newCentroids.Clear()

        knn.basics.knnQT.queryPoints = New List(Of cv.Point2f)(queryPoints)
        knn.Run(src)

        Dim matches As New List(Of cv.Point2f)(knn.matchedPoints)
        If matches IsNot Nothing Then ' first pass condition.
            For i = 0 To matches.Count - 1
                If matches(i).X < 0 Then
                    For j = 0 To kalman.Count - 1
                        If kalman(j).kInput(0) < 0 Then
                            kalman(j).kInput = {knn.basics.knnQT.queryPoints(i).X, knn.basics.knnQT.queryPoints(i).Y, 0, 0, 0, 0}
                            Exit For
                        End If
                    Next
                End If
            Next

            If queryMasks.Count > 0 Then dst1.SetTo(0)
            Dim inputRect = New cv.Rect
            drawRC.viewObjects.Clear()
            Dim useDrawRC = drawRCCheck.checked = False
            For i = 0 To knn.basics.knnQT.trainingPoints.Count - 1
                inputRect = New cv.Rect(kalman(i).kInput(2), kalman(i).kInput(3), kalman(i).kInput(4), kalman(i).kInput(5))
                Dim pt1 = knn.basics.knnQT.trainingPoints(i)
                Dim matchIndex = -1
                If matches.Contains(pt1) Then
                    matchIndex = matches.IndexOf(pt1)
                    inputRect = queryRects(matchIndex)
                    kalman(i).kInput = {queryPoints(matchIndex).X, queryPoints(matchIndex).Y, inputRect.X, inputRect.Y, inputRect.Width, inputRect.Height}
                    If task.useKalman Then
                        kalman(i).Run(src)
                    Else
                        kalman(i).kOutput = {queryPoints(matchIndex).X, queryPoints(matchIndex).Y, inputRect.X, inputRect.Y, inputRect.Width, inputRect.Height}
                    End If

                    Dim vo = New viewObject
                    vo.centroid = New cv.Point(kalman(i).kOutput(0), kalman(i).kOutput(1))

                    Dim outRect = New cv.Rect(kalman(i).kOutput(2), kalman(i).kOutput(3), kalman(i).kOutput(4), kalman(i).kOutput(5))
                    If outRect.X < 0 Then outRect.X = 0
                    If outRect.Y < 0 Then outRect.Y = 0
                    If outRect.X + outRect.Width > src.Width Then outRect.Width = src.Width - outRect.X
                    If outRect.Y + outRect.Height > src.Height Then outRect.Height = src.Height - outRect.Y
                    If outRect.Width < 0 Then outRect.Width = 1
                    If outRect.Height < 0 Then outRect.Height = 1
                    vo.rectInHist = outRect

                    Dim minPixels = pixelSlider.value

                    If vo.rectInHist.Width * vo.rectInHist.Height >= minPixels Then
                        Dim pt = vo.centroid
                        If pt.X < 0 Then pt.X = 0
                        If pt.X >= src.Width Then pt.X = src.Width - 1
                        If pt.Y < 0 Then pt.Y = 0
                        If pt.Y >= src.Height Then pt.Y = src.Height - 1

                        vo.preKalmanRect = inputRect
                        If matchIndex < queryMasks.Count Then
                            If queryMasks(matchIndex).Size <> src.Size Then vo.mask = queryMasks(matchIndex) Else vo.mask = queryMasks(matchIndex)(vo.preKalmanRect)
                        End If

                        vo.LayoutColor = (i + 5) Mod 255
                        If floodPoints.Count > 0 Then vo.floodPoint = floodPoints(matchIndex)
                        drawRC.viewObjects.Add(inputRect.Width * inputRect.Height, vo)
                    End If
                End If
            Next

            If useDrawRC Then
                drawRC.Run(src)
                dst1 = drawRC.dst1
            End If
        End If
    End Sub
End Class






Public Class KNN_1_to_1FIFO : Inherits VBparent
    Public neighbors As New cv.Mat
    Public lastSet As New List(Of cv.Point2f)
    Public currSet As New List(Of cv.Point2f)
    Public knn As cv.ML.KNearest
    Dim random As New Random_Basics
    Public Sub New()
        random.rangeRect = New cv.Rect(0, 0, dst1.Width, dst1.Height)
        label1 = "White=TrainingData, Red=queries"
        knn = cv.ML.KNearest.Create()
        task.desc = "Using the last set of points, find the nearest point for each the current set - first come, first served."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        dst1.SetTo(cv.Scalar.Black)

        If standalone Then
            random.Run(Nothing)
            lastSet = New List(Of cv.Point2f)(random.Points2f)
            random.Run(Nothing)
            currSet = New List(Of cv.Point2f)(random.Points2f)
        End If

        Dim queries = New cv.Mat(currSet.Count, 2, cv.MatType.CV_32F, currSet.ToArray)

        For i = 0 To currSet.Count - 1
            Dim trainData = New cv.Mat(lastSet.Count, 2, cv.MatType.CV_32F, lastSet.ToArray)
            Dim responses = New List(Of Integer)(Enumerable.Range(start:=0, count:=lastSet.Count).Select(Function(x) x))
            Dim response = New cv.Mat(trainData.Rows, 1, cv.MatType.CV_32S, responses.ToArray)
            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            knn.FindNearest(queries.Row(i), responses.Count, New cv.Mat, neighbors) ' rank each point with each point in the lastSet
            Dim index = neighbors.Get(Of Single)(0, 0)
            responses.RemoveAt(index)
            lastSet.RemoveAt(index)
            Dim pt = trainData.Get(Of cv.Point2f)(index, 0)
            Dim qpoint = currSet(i)
            cv.Cv2.Circle(dst1, qpoint, task.dotSize, cv.Scalar.Red, -1, task.lineType, 0)
            dst1.Line(pt, qpoint, cv.Scalar.Red, task.lineSize, task.lineType)
            cv.Cv2.Circle(dst1, pt, task.dotSize, cv.Scalar.White, -1, task.lineType, 0)
        Next
    End Sub
End Class
