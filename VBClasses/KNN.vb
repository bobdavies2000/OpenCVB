Imports System.Runtime.InteropServices
Imports VBClasses
Imports cv = OpenCvSharp
Public Class KNN_Basics : Inherits TaskParent
    Public knn2 As New KNN_Minimal
    Public ptListTrain As New List(Of cv.Point)
    Public ptListQuery As New List(Of cv.Point)
    Public trainInput As New List(Of cv.Point2f)
    Public queries As New List(Of cv.Point2f)
    Public result(,) As Integer ' Get results here...
    Public Sub New()
        desc = "Default normalized KNN with dimension 2"
    End Sub
    Public Shared Function displayResults(result(,) As Integer, queries As List(Of cv.Point2f), trainInput As List(Of cv.Point2f)) As cv.Mat
        Dim dst = New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)
        For i = 0 To queries.Count - 1
            Dim pt = queries(i)
            Dim nn = trainInput(result(i, 0))
            dst.Circle(pt, task.DotSize + 4, cv.Scalar.Yellow, -1, task.lineType)
            dst.Line(pt, nn, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next

        For Each pt In trainInput
            dst.Circle(pt, task.DotSize + 4, cv.Scalar.Red, -1, task.lineType)
        Next
        Return dst
    End Function
    Public Shared Function getResults(neighbors As cv.Mat, rows As Integer, cols As Integer) As Integer(,)
        Dim nData(rows * cols - 1) As Single
        Marshal.Copy(neighbors.Data, nData, 0, nData.Length)

        Dim result(rows - 1, cols - 1) As Integer
        For i = 0 To rows - 1
            For j = 0 To cols - 1
                Dim test = nData(i * cols + j)
                If test < nData.Length And test >= 0 Then result(i, j) = CInt(nData(i * cols + j))
            Next
        Next
        Return result
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim KNNdimension = 2
        If standalone Then
            Static random As New Random_Basics
            If task.heartBeatLT Then
                random.Run(src)
                trainInput = New List(Of cv.Point2f)(random.PointList)
            End If
            random.Run(src)
            queries = New List(Of cv.Point2f)(random.PointList)
        End If

        If ptListQuery.Count > 0 Then
            trainInput.Clear()
            For Each pt In ptListTrain
                trainInput.Add(New cv.Point2f(pt.X, pt.Y))
            Next

            queries.Clear()
            For Each pt In ptListQuery
                queries.Add(New cv.Point2f(pt.X, pt.Y))
            Next
        End If

        knn2.trainMat = cv.Mat.FromPixelData(trainInput.Count, KNNdimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn2.queryMat = cv.Mat.FromPixelData(queries.Count, KNNdimension, cv.MatType.CV_32F, queries.ToArray)
        If knn2.trainMat.Rows > 0 And knn2.queryMat.Rows > 0 Then
            knn2.Run(src)
            result = knn2.result
            If standalone Then dst2 = KNN_Basics.displayResults(result, queries, trainInput)
        End If
    End Sub
End Class






Public Class KNN_Minimal : Inherits TaskParent
    Implements IDisposable
    Public knn As cv.ML.KNearest
    Public result(,) As Integer ' Get results here...
    Public queryMat As New cv.Mat
    Public trainMat As New cv.Mat
    Dim neighbors As New cv.Mat
    Public options As New Options_KNN
    Public Sub New()
        knn = cv.ML.KNearest.Create()
        desc = "The bare minimum implementation of KNN - independent of dimension."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If queryMat.Rows = 0 Or trainMat.Rows = 0 Then
            SetTrueText("The queryMat or trainMat (or both) are empty.  Try again...")
            Exit Sub
        End If

        Dim responseList As IEnumerable(Of Integer) = Enumerable.Range(0, 10).Select(Function(x) x)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "_Test algorithm")
            Exit Sub
        End If

        Dim response = cv.Mat.FromPixelData(trainMat.Rows, 1, cv.MatType.CV_32S, Enumerable.Range(start:=0, trainMat.Rows).ToArray)

        Dim trainNormalized As New cv.Mat, queryNormalized As New cv.Mat
        cv.Cv2.Normalize(trainMat, trainNormalized, 1, 0, cv.NormTypes.MinMax)
        knn.Train(trainNormalized, cv.ML.SampleTypes.RowSample, response)

        cv.Cv2.Normalize(queryMat, queryNormalized, 1, 0, cv.NormTypes.MinMax)
        knn.FindNearest(queryNormalized, trainMat.Rows, New cv.Mat, neighbors)

        If neighbors.Rows > 0 Then result = KNN_Basics.getResults(neighbors, queryMat.Rows, trainMat.Rows)
    End Sub
    Protected Overrides Sub Finalize()
        If knn IsNot Nothing Then knn.Dispose()
    End Sub
End Class




Public Class KNN_IndividualQuery : Inherits TaskParent
    Implements IDisposable
    Public knn As cv.ML.KNearest
    Public result(,) As Integer ' Get results here...
    Public queryMat As New cv.Mat
    Public trainMat As New cv.Mat
    Public dimension As Integer
    Dim neighbors As New cv.Mat
    Public Sub New()
        knn = cv.ML.KNearest.Create()
        desc = "The bare minimum implementation of KNN."
    End Sub
    Public Sub runTestData()
        knn.FindNearest(queryMat, trainMat.Rows, New cv.Mat, neighbors)
        result = KNN_Basics.getResults(neighbors, queryMat.Rows, trainMat.Rows)
    End Sub

    Public Function runQueryBest(query As cv.Mat) As Integer
        knn.FindNearest(query, trainMat.Rows, New cv.Mat, neighbors)
        Return neighbors.Get(Of Single)(0, 0)
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim responseList As IEnumerable(Of Integer) = Enumerable.Range(0, 10).Select(Function(x) x)
        If standaloneTest() Then
            SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "_Test algorithm")
            Exit Sub
        End If

        Dim response = cv.Mat.FromPixelData(trainMat.Rows, 1, cv.MatType.CV_32S, Enumerable.Range(start:=0, trainMat.Rows).ToArray)

        knn.Train(trainMat, cv.ML.SampleTypes.RowSample, response)
    End Sub
    Protected Overrides Sub Finalize()
        If knn IsNot Nothing Then knn.Dispose()
    End Sub
End Class





Public Class KNN_Farthest : Inherits TaskParent
    Public knn As New KNN_Minimal
    Public lpFar As lpData
    Public trainInput As New List(Of cv.Point2f)
    Public queries As New List(Of cv.Point2f)
    Public Sub New()
        labels = {"", "", "Lines connecting pairs that are farthest.", "Training Input which is also query input and longest line"}
        desc = "Use KNN to find the farthest point from each query point."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then
            If task.heartBeatLT Then
                Static random As New Random_Basics
                random.Run(src)
                trainInput = New List(Of cv.Point2f)(random.PointList)
                queries = New List(Of cv.Point2f)(trainInput)
            End If
        End If

        Dim dimension = 2
        knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)
        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn.Run(src)

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim farthest = New List(Of lpData)
        Dim distances As New List(Of Single)
        For i = 0 To knn.result.GetUpperBound(0) - 1
            Dim farIndex = knn.result(i, knn.result.GetUpperBound(1))
            Dim lp = New lpData(queries(i), trainInput(farIndex))
            dst2.Circle(lp.p1, task.DotSize + 4, cv.Scalar.Yellow, -1, task.lineType)
            dst2.Circle(lp.p2, task.DotSize + 4, cv.Scalar.Yellow, -1, task.lineType)
            dst2.Line(lp.p1, lp.p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
            farthest.Add(lp)
            distances.Add(lp.p1.DistanceTo(lp.p2))
        Next

        For Each pt In queries
            dst3.Circle(pt, task.DotSize + 4, cv.Scalar.Red, -1, task.lineType)
        Next

        Dim maxIndex = distances.IndexOf(distances.Max())
        lpFar = farthest(maxIndex)
        dst3.Line(lpFar.p1, lpFar.p2, white, task.lineWidth, task.lineType)
    End Sub
End Class








Public Class KNN_OneToOne : Inherits TaskParent
    Public matches As New List(Of lpData)
    Public noMatch As New List(Of cv.Point)
    Public knn As New KNN_Basics
    Public queries As New List(Of cv.Point2f)
    Dim random As New Random_Basics
    Public Sub New()
        labels(2) = "KNN_OneToOne output with just the closest match.  Red = training data, yellow = queries."
        desc = "Map points 1:1 with neighbor.  Keep only the nearest."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            If task.heartBeatLT Then
                random.Run(src)
                knn.trainInput = New List(Of cv.Point2f)(random.PointList)
                random.Run(src)
                queries = New List(Of cv.Point2f)(random.PointList)
            End If
        End If

        knn.queries = queries
        knn.trainInput = New List(Of cv.Point2f)(queries)
        knn.Run(src)

        Dim nearest As New List(Of Integer)
        ' map the points 1 to 1: find duplicates, choose which is better.
        ' loser must relinquish the training data element
        Dim sortedResults As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
        For i = 0 To queries.Count - 1
            nearest.Add(knn.result(i, 0))
            sortedResults.Add(knn.result(i, 0), i)
        Next

        ' we are comparing each element to the next so -2
        For i = 0 To Math.Min(sortedResults.Count, knn.trainInput.Count) - 2
            Dim resultA = sortedResults.ElementAt(i).Key
            Dim resultB = sortedResults.ElementAt(i + 1).Key
            If resultA = resultB Then
                Dim nn = knn.trainInput(resultA)
                Dim queryA = sortedResults.ElementAt(i).Value
                For j = i + 1 To sortedResults.Count - 1
                    resultB = sortedResults.ElementAt(j).Key
                    If resultA <> resultB Then Exit For
                    Dim queryB = sortedResults.ElementAt(j).Value
                    Dim p1 = queries(queryA)
                    Dim p2 = queries(queryB)
                    Dim distance1 = Math.Sqrt((p1.X - nn.X) * (p1.X - nn.X) + (p1.Y - nn.Y) * (p1.Y - nn.Y))
                    Dim distance2 = Math.Sqrt((p2.X - nn.X) * (p2.X - nn.X) + (p2.Y - nn.Y) * (p2.Y - nn.Y))
                    If distance1 < distance2 Then
                        nearest(queryB) = -1
                    Else
                        nearest(queryA) = -1
                        queryA = queryB
                    End If
                Next
            End If
        Next

        dst2.SetTo(0)
        For Each pt In knn.trainInput
            dst2.Circle(pt, task.DotSize + 4, cv.Scalar.Red, -1, task.lineType)
        Next

        noMatch.Clear()
        matches.Clear()
        For i = 0 To queries.Count - 1
            Dim pt = queries(i)
            dst2.Circle(pt, task.DotSize + 4, cv.Scalar.Yellow, -1, task.lineType)
            If nearest(i) = -1 Then
                noMatch.Add(pt)
            Else
                If nearest(i) < knn.trainInput.Count Then ' there seems like a boundary condition when there is only 1 traininput...
                    Dim nn = knn.trainInput(nearest(i))
                    matches.Add(New lpData(pt, nn))
                    dst2.Line(nn, pt, white, task.lineWidth, task.lineType)
                End If
            End If
        Next
        If standaloneTest() = False Then knn.trainInput = New List(Of cv.Point2f)(queries)
    End Sub
End Class





Public Class NR_KNN_MaxDistance : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public outputPoints As New List(Of (cv.Point2f, cv.Point2f))
    Public options As New Options_KNN
    Dim perif As New FeatureMap_Periphery
    Public Sub New()
        desc = "Use the feature points on the periphery and find the points farthest from each."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        Static pairList As New List(Of (cv.Point2f, cv.Point2f))

        perif.Run(src)
        dst3 = perif.dst3
        If perif.fcs.feat.features.Count = 0 Then Exit Sub

        knn.queries = perif.ptOutside
        knn.trainInput = knn.queries
        knn.Run(emptyMat)

        dst2 = src
        For i = 0 To knn.result.GetUpperBound(0)
            Dim lp = New lpData(knn.queries(knn.result(i, knn.queries.Count - 1)),
                                knn.queries(knn.result(i, 0)))
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
        Next

        labels(2) = "There were " + CStr(perif.fcs.feat.features.Count) + " features and " +
                                    CStr(knn.queries.Count) + " were on the periphery."
    End Sub
End Class





Public Class KNN_MinDistance : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public outputPoints2f As New List(Of cv.Point2f)
    Public outputPoints As New List(Of cv.Point)
    Dim options As New Options_Features
    Dim feat As New Feature_Basics
    Public Sub New()
        If standalone Then task.fOptions.FeatureMethod.SelectedItem = "AGAST"
        desc = "Enforce a minimum distance to the next feature."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        feat.Run(task.gray)
        labels = feat.labels

        If feat.features.Count = 0 Then Exit Sub

        knn.ptListQuery = New List(Of cv.Point)(feat.features)
        knn.ptListTrain = knn.ptListQuery
        knn.Run(src)

        dst3.SetTo(0)
        For Each pt In feat.features
            dst3.Circle(pt, task.DotSize, cv.Scalar.White, -1, task.lineType)
        Next
        labels(3) = "There were " + CStr(feat.features.Count) + " points in the input"

        Dim tooClose As New List(Of (cv.Point2f, cv.Point2f))
        For i = 0 To knn.result.GetUpperBound(0)
            For j = 1 To knn.result.GetUpperBound(1)
                Dim p1 = knn.queries(knn.result(i, j))
                Dim p2 = knn.queries(knn.result(i, j - 1))
                If p1.DistanceTo(p2) > options.minDistance Then Exit For
                If tooClose.Contains((p2, p1)) = False Then tooClose.Add((p1, p2))
            Next
        Next

        For Each tuple In tooClose
            Dim p1 = tuple.Item1
            Dim p2 = tuple.Item2
            Dim pt = If(p1.X <= p2.X, p1, p2)  ' trim the point with lower x to avoid flickering...
            If p1.X = p2.X Then pt = If(p1.Y <= p2.Y, p1, p2)
            If knn.queries.Contains(pt) Then knn.queries.RemoveAt(knn.queries.IndexOf(pt))
        Next

        dst2 = src
        outputPoints.Clear()
        outputPoints2f.Clear()
        For Each pt In knn.queries
            dst2.Circle(pt, task.DotSize + 2, cv.Scalar.White, -1, task.lineType)
            outputPoints.Add(pt)
            outputPoints2f.Add(pt)
        Next
        labels(2) = "After filtering for min distance = " + CStr(options.minDistance) + " there are " +
                        CStr(knn.queries.Count) + " points"
    End Sub
End Class






Public Class KNN_Grid : Inherits TaskParent
    Dim knn As New KNN_Minimal
    Dim fLess As New FeatureLess_DepthFull
    Public trainInput As New List(Of cv.Point3f)
    Public queries As New List(Of cv.Point3f)
    Public Sub New()
        desc = "Use FeatureLess_DepthFull grid elements to define the clusters for all remaining pixels."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        fLess.Run(task.gray)
        dst2 = fLess.dst2
        labels(2) = fLess.labels(2)

        Dim clusters As New List(Of Byte)
        trainInput.Clear()
        queries.Clear()
        For Each r In task.gridRects
            Dim val = fLess.dst2.Get(Of Byte)(r.TopLeft.Y, r.TopLeft.X)
            If val > 0 Then
                trainInput.Add(New cv.Vec3f(r.TopLeft.X, r.TopLeft.Y, task.gray(r).Mean()(0)))
                clusters.Add(val)
            Else
                For y = 0 To r.Height - 1
                    For x = 0 To r.Width - 1
                        queries.Add(New cv.Vec3f(r.TopLeft.X + x, r.TopLeft.Y + y, task.gray(r).Mean()(0)))
                    Next
                Next
            End If
        Next

        Dim dimension = 3
        knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)
        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn.Run(emptyMat)

        For i = 0 To queries.Count - 1
            Dim index = knn.result(i, 0)
            Dim vecTrain = trainInput(index)
            Dim entry = fLess.dst2.Get(Of Byte)(vecTrain.Y, vecTrain.Z)
            Dim vecTest = queries(i)
            dst2.Set(Of Byte)(vecTest.Y, vecTest.X, entry)
        Next
    End Sub
End Class






Public Class KNN_Dimension2 : Inherits TaskParent
    Public knn As New KNN_Minimal
    Dim random As New Random_Basics
    Dim trainInput As New List(Of cv.Point2f)
    Dim queries As New List(Of cv.Point2f)
    Public Sub New()
        OptionParent.FindSlider("Random Pixel Count").Value = 10
        desc = "Test knn with random 2D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim dimension = 2

        If task.heartBeat Then
            random.Run(src)
            trainInput = New List(Of cv.Point2f)(random.PointList)
        End If

        random.Run(src)
        queries = New List(Of cv.Point2f)(random.PointList)
        knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)

        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn.Run(src)
        dst2 = KNN_Basics.displayResults(knn.result, queries, trainInput)

        labels(2) = "The top " + CStr(trainInput.Count) + " best matches are shown. Red=TrainingData, yellow = queries"
    End Sub
End Class







Public Class KNN_Dimension3 : Inherits TaskParent
    Dim knn As New KNN_Minimal
    Dim dist As New Distance_Point3D
    Dim random As New Random_Basics3D
    Public trainInput As New List(Of cv.Point3f)
    Public queries As New List(Of cv.Point3f)
    Public Sub New()
        labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
        OptionParent.FindSlider("Random Pixel Count").Value = 100
        desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then
            queries.Clear()
            trainInput.Clear()
            random.Run(src)
            For Each pt In random.PointList
                Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                If trainInput.Count = 10 Then
                    If vec.Z Then
                        queries.Add(pt)
                        Exit For
                    End If
                Else
                    If vec.Z Then trainInput.Add(pt)
                End If
            Next
        End If

        Dim dimension = 3
        knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)
        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn.Run(src)

        dst2.SetTo(0)
        dist.inPoint1 = queries(0)
        For i = 0 To trainInput.Count - 1
            Dim pt = New cv.Point2f(trainInput(i).X, trainInput(i).Y)
            dst2.Circle(pt, task.DotSize, cv.Scalar.Red, -1, task.lineType)
            dist.inPoint2 = trainInput(i)
            dist.Run(src)
            SetTrueText("depth=" + CStr(trainInput(i).Z) + vbCrLf + "dist=" + Format(dist.distance, fmt0), pt)
        Next
        For i = 0 To queries.Count - 1
            Dim pt = New cv.Point2f(queries(i).X, queries(i).Y)
            For j = 0 To Math.Min(2, trainInput.Count) - 1
                Dim index = knn.result(i, j)
                Dim nn = New cv.Point2f(trainInput(index).X, trainInput(index).Y)
                dst2.Circle(pt, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                dst2.Line(pt, nn, cv.Scalar.Yellow, task.lineWidth, task.lineType)
                Dim midPt = New cv.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                SetTrueText(CStr(j), midPt)
                SetTrueText("depth=" + CStr(queries(i).Z), pt)
            Next
        Next
    End Sub
End Class








Public Class KNN_Dimension4 : Inherits TaskParent
    Dim knn As New KNN_Minimal
    Dim dist As New Distance_Point4D
    Dim random As New Random_Basics4D
    Public trainInput As New List(Of cv.Vec4f)
    Public queries As New List(Of cv.Vec4f)
    Public Sub New()
        labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
        OptionParent.FindSlider("Random Pixel Count").Value = 5
        desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then
            random.Run(src)
            trainInput = New List(Of cv.Vec4f)(random.PointList)
            queries.Clear()
            queries.Add(New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height)))
        End If

        Dim dimension = 4
        knn.queryMat = cv.Mat.FromPixelData(queries.Count, dimension, cv.MatType.CV_32F, queries.ToArray)
        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count, dimension, cv.MatType.CV_32F, trainInput.ToArray)
        knn.Run(src)

        dst2.SetTo(0)
        dist.inPoint1 = queries(0)
        For i = 0 To trainInput.Count - 1
            Dim pt = New cv.Point2f(trainInput(i)(0), trainInput(i)(1))
            dst2.Circle(pt, task.DotSize, cv.Scalar.Red, -1, task.lineType)
            dist.inPoint2 = trainInput(i)
            dist.Run(src)
            SetTrueText("dist=" + Format(dist.distance, fmt0), pt)
        Next
        For i = 0 To queries.Count - 1
            Dim pt = New cv.Point2f(queries(i)(0), queries(i)(1))
            For j = knn.result.GetLowerBound(1) To knn.result.GetUpperBound(1)
                Dim index = knn.result(i, j)
                Dim nn = New cv.Point2f(trainInput(index)(0), trainInput(index)(1))
                dst2.Circle(pt, task.DotSize, cv.Scalar.Yellow, -1, task.lineType)
                dst2.Line(pt, nn, task.highlight, task.lineWidth, task.lineType)
                Dim midPt = New cv.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                SetTrueText(CStr(j), midPt)
            Next
        Next
    End Sub
End Class





Public Class KNN_DimensionN : Inherits TaskParent
    Dim knn As New KNN_Minimal
    Public trainInput As New List(Of Single)
    Public queries As New List(Of Single)
    Public Sub New()
        labels(2) = "Highlight color (Yellow) is query.  The red dots are the training set."
        desc = "Test the use of the general form KNN_BasicsN algorithm"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeatLT Then
            trainInput.Clear()
            For i = 0 To knn.options.numPoints - 1
                For j = 0 To knn.options.knnDimension - 1
                    trainInput.Add(msRNG.Next(dst2.Height))
                Next
            Next

            queries.Clear()
            For j = 0 To knn.options.knnDimension - 1
                queries.Add(msRNG.Next(dst2.Height))
            Next
        End If

        knn.queryMat = cv.Mat.FromPixelData(queries.Count \ knn.options.knnDimension, knn.options.knnDimension, cv.MatType.CV_32F,
                                            queries.ToArray)
        knn.trainMat = cv.Mat.FromPixelData(trainInput.Count \ knn.options.knnDimension, knn.options.knnDimension, cv.MatType.CV_32F,
                                            trainInput.ToArray)
        knn.Run(src)
        dst2.SetTo(0)
        For i = 0 To trainInput.Count \ knn.options.knnDimension - 1 Step knn.options.knnDimension
            Dim pt = New cv.Point2f(trainInput(i), trainInput(i + 1))
            dst2.Circle(pt, task.DotSize, cv.Scalar.Red, -1, task.lineType)
        Next

        For i = 0 To queries.Count \ knn.options.knnDimension - 1 Step knn.options.knnDimension
            Dim pt = New cv.Point2f(queries(i), queries(i + 1))
            Dim index = knn.result(i, 0) * knn.options.knnDimension
            Dim nn = New cv.Point2f(trainInput(index), trainInput(index + 1))
            dst2.Circle(pt, task.DotSize + 1, task.highlight, -1, task.lineType)
            dst2.Line(pt, nn, task.highlight, task.lineWidth, task.lineType)
        Next
        If standaloneTest() Then
            SetTrueText("Results are easily verified for the 2-dimensional case.  For higher dimension, " + vbCrLf +
                            "the results may appear incorrect because the higher dimensions are projected into " + vbCrLf +
                            "a 2-dimensional presentation.", 3)
        End If
    End Sub
End Class