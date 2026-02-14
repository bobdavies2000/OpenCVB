Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class KNN_Basics : Inherits TaskParent
        Public knn2 As New KNN_N2Basics
        Public ptListTrain As New List(Of cv.Point)
        Public ptListQuery As New List(Of cv.Point)
        Public trainInput As New List(Of cv.Point2f)
        Public queries As New List(Of cv.Point2f)
        Public neighbors As New List(Of List(Of Integer))
        Public result(,) As Integer ' Get results here...
        Public desiredMatches As Integer = -1 ' -1 indicates it is to use the number of queries.
        Public Sub New()
            desc = "Default unnormalized KNN with dimension 2"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                Static bPoint As New BrickPoint_Basics
                bPoint.Run(src)

                trainInput.Clear()
                For Each pt In bPoint.ptList
                    trainInput.Add(New cv.Point2f(pt.X, pt.Y))
                Next
                queries = trainInput
            End If

            If ptListTrain.Count > 0 Then
                trainInput.Clear()
                For Each pt In ptListTrain
                    trainInput.Add(New cv.Point2f(pt.X, pt.Y))
                Next

                queries.Clear()
                For Each pt In ptListQuery
                    queries.Add(New cv.Point2f(pt.X, pt.Y))
                Next
            End If

            knn2.trainInput = trainInput
            knn2.queries = queries
            knn2.desiredMatches = desiredMatches
            knn2.Run(src)
            neighbors = knn2.neighbors
            result = knn2.result

            If standaloneTest() Then
                dst2 = task.color
                For Each pt In trainInput
                    DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Red)
                    dst2.Circle(pt, task.DotSize, task.highlight)
                Next
            End If
        End Sub
    End Class






    Public Class KNN_N2Basics : Inherits TaskParent
        Implements IDisposable
        Public knn As cv.ML.KNearest
        Public trainInput As New List(Of cv.Point2f) ' put training data here
        Public queries As New List(Of cv.Point2f) ' put Query data here
        Public neighbors As New List(Of List(Of Integer))
        Public result(,) As Integer ' Get results here...
        Public desiredMatches As Integer = -1 ' -1 indicates it is to use the number of queries.
        Public Sub New()
            knn = cv.ML.KNearest.Create()
            labels(2) = "Red=TrainingData, yellow = queries"
            desc = "Train a KNN model and map each query to the nearest training neighbor."
        End Sub
        Public Sub displayResults()
            dst2.SetTo(0)
            For i = 0 To queries.Count - 1
                Dim pt = queries(i)
                Dim test = result(i, 0)
                If test >= trainInput.Count Or test < 0 Then Continue For
                Dim nn = trainInput(result(i, 0))
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
            Next

            For Each pt In trainInput
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim KNNdimension = 2

            If standalone Then
                Static random As New Random_Basics
                If task.heartBeat Then
                    random.Run(src)
                    trainInput = New List(Of cv.Point2f)(random.PointList)
                End If
                random.Run(src)
                queries = New List(Of cv.Point2f)(random.PointList)
            End If

            Dim queryMat = cv.Mat.FromPixelData(queries.Count, KNNdimension, cv.MatType.CV_32F, queries.ToArray)
            If queryMat.Rows = 0 Then
                SetTrueText("There were no queries provided.  There is nothing to do...")
                Exit Sub
            End If

            If trainInput.Count = 0 Then trainInput = New List(Of cv.Point2f)(queries) ' first pass, just match the queries.
            Dim trainData = cv.Mat.FromPixelData(trainInput.Count, KNNdimension, cv.MatType.CV_32F, trainInput.ToArray)
            Dim response = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            Dim neighborMat As New cv.Mat

            Dim dm = If(desiredMatches < 0, trainInput.Count, desiredMatches)
            knn.FindNearest(queryMat, dm, New cv.Mat, neighborMat)
            If neighborMat.Rows <> queryMat.Rows Or neighborMat.Cols <> dm Then
                Debug.WriteLine("KNN's FindNearest did not return the correct number of neighbors.  Marshal.copy will fail so exit.")
                Exit Sub
            End If

            Dim nData(queryMat.Rows * dm - 1) As Single
            If nData.Length = 0 Then Exit Sub
            Marshal.Copy(neighborMat.Data, nData, 0, nData.Length)

            For i = 0 To nData.Count - 1
                If Math.Abs(nData(i)) > trainInput.Count Then nData(i) = 0 ' value must be within the range of traininput
            Next

            ReDim result(queryMat.Rows - 1, dm - 1)
            neighbors.Clear()
            For i = 0 To queryMat.Rows - 1
                Dim pt = queries(i)
                Dim res = New List(Of Integer)
                For j = 0 To dm - 1
                    Dim test = nData(i * dm + j)
                    If test < nData.Length And test >= 0 Then
                        result(i, j) = CInt(nData(i * dm + j))
                        Dim index = nData(i * dm + j)
                        res.Add(index)
                    End If
                Next
                neighbors.Add(res)
            Next
            If standaloneTest() Then displayResults()
        End Sub
        Protected Overrides Sub Finalize()
            If knn IsNot Nothing Then knn.Dispose()
        End Sub
    End Class





    Public Class NR_KNN_N2BasicsTest : Inherits TaskParent
        Public knn As New KNN_Basics
        Dim random As New Random_Basics
        Public Sub New()
            OptionParent.FindSlider("Random Pixel Count").Value = 10
            desc = "Test knn with random 2D points in the image.  Find the nearest requested neighbors."
        End Sub
        Public Sub accumulateDisplay()
            Dim dm = Math.Min(knn.trainInput.Count, knn.queries.Count)
            For i = 0 To knn.queries.Count - 1
                Dim pt = knn.queries(i)
                Dim test = knn.result(i, 0)
                If test >= knn.trainInput.Count Or test < 0 Then Continue For
                Dim nn = knn.trainInput(knn.result(i, 0))
                DrawCircle(dst3, pt, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst3, pt, nn, cv.Scalar.Yellow)
            Next

            For Each pt In knn.trainInput
                DrawCircle(dst3, pt, task.DotSize + 4, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeat Then
                dst3.SetTo(0)
                random.Run(src)
                knn.trainInput = New List(Of cv.Point2f)(random.PointList)
            End If
            random.Run(src)
            knn.queries = New List(Of cv.Point2f)(random.PointList)

            knn.Run(src)
            knn.knn2.displayResults()
            dst2 = knn.knn2.dst2
            accumulateDisplay()

            labels(2) = "The top " + CStr(knn.trainInput.Count) + " best matches are shown. Red=TrainingData, yellow = queries"
        End Sub
    End Class







    Public Class KNN_N3Basics : Inherits TaskParent
        Implements IDisposable
        Public knn As cv.ML.KNearest
        Public trainInput As New List(Of cv.Point3f) ' put training data here
        Public queries As New List(Of cv.Point3f) ' put Query data here
        Public result(,) As Integer ' Get results here...
        Public Sub New()
            knn = cv.ML.KNearest.Create()
            desc = "Use knn with the input 3D points in the image.  Find the nearest neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "Test algorithm")
                Exit Sub
            End If

            Dim KNNdimension = 3
            Dim queryMat = cv.Mat.FromPixelData(queries.Count, KNNdimension, cv.MatType.CV_32F, queries.ToArray)
            If queryMat.Rows = 0 Then
                SetTrueText("There were no queries provided.  There is nothing to do...")
                Exit Sub
            End If

            If trainInput.Count = 0 Then trainInput = New List(Of cv.Point3f)(queries) ' first pass, just match the queries.
            Dim trainData = cv.Mat.FromPixelData(trainInput.Count, KNNdimension, cv.MatType.CV_32F, trainInput.ToArray)
            Dim response = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            Dim neighbors As New cv.Mat
            Dim dm = trainInput.Count
            knn.FindNearest(queryMat, dm, New cv.Mat, neighbors)

            Dim nData(queryMat.Rows * dm - 1) As Single
            Marshal.Copy(neighbors.Data, nData, 0, nData.Length)

            ReDim result(queryMat.Rows - 1, dm - 1)
            For i = 0 To queryMat.Rows - 1
                For j = 0 To dm - 1
                    Dim test = nData(i * dm + j)
                    If test < nData.Length And test >= 0 Then result(i, j) = CInt(nData(i * dm + j))
                Next
            Next
        End Sub
        Protected Overrides Sub Finalize()
            If knn IsNot Nothing Then knn.Dispose()
        End Sub
    End Class






    Public Class KNN_N4Basics : Inherits TaskParent
        Implements IDisposable
        Public knn As cv.ML.KNearest
        Public trainInput As New List(Of cv.Vec4f) ' put training data here
        Public queries As New List(Of cv.Vec4f) ' put Query data here
        Public result(,) As Integer ' Get results here...
        Public Sub New()
            knn = cv.ML.KNearest.Create()
            labels(2) = "Red=TrainingData, yellow = queries, text shows Z distance to that point from query point"
            desc = "Use knn with the input 4D points in the image.  Find the nearest neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standaloneTest() Then
                SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "Test algorithm")
                Exit Sub
            End If

            Dim KNNdimension = 4
            Dim queryMat = cv.Mat.FromPixelData(queries.Count, KNNdimension, cv.MatType.CV_32F, queries.ToArray)
            If queryMat.Rows = 0 Then
                SetTrueText("There were no queries provided.  There is nothing to do...")
                Exit Sub
            End If

            If trainInput.Count = 0 Then trainInput = New List(Of cv.Vec4f)(queries) ' first pass, just match the queries.
            Dim trainData = cv.Mat.FromPixelData(trainInput.Count, KNNdimension, cv.MatType.CV_32F, trainInput.ToArray)
            Dim response = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S, Enumerable.Range(start:=0, trainData.Rows).ToArray)
            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            Dim neighbors As New cv.Mat
            Dim dm = trainInput.Count
            knn.FindNearest(queryMat, dm, New cv.Mat, neighbors)

            Dim nData(queryMat.Rows * dm - 1) As Single
            Marshal.Copy(neighbors.Data, nData, 0, nData.Length)

            ReDim result(queryMat.Rows - 1, dm - 1)
            For i = 0 To queryMat.Rows - 1
                For j = 0 To dm - 1
                    Dim test = nData(i * dm + j)
                    If test < nData.Length And test >= 0 Then result(i, j) = CInt(nData(i * dm + j))
                Next
            Next
        End Sub
        Protected Overrides Sub Finalize()
            If knn IsNot Nothing Then knn.Dispose()
        End Sub
    End Class







    Public Class NR_KNN_N3BasicsTest : Inherits TaskParent
        Dim knn As New KNN_N3Basics
        Dim dist As New Distance_Point3D
        Dim random As New Random_Basics3D
        Public Sub New()
            labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
            OptionParent.FindSlider("Random Pixel Count").Value = 100
            desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeat Then
                knn.queries.Clear()
                knn.trainInput.Clear()
                random.Run(src)
                For Each pt In random.PointList
                    Dim vec = task.pointCloud.Get(Of cv.Point3f)(pt.Y, pt.X)
                    If knn.trainInput.Count = 10 Then
                        If vec.Z Then
                            knn.queries.Add(pt)
                            Exit For
                        End If
                    Else
                        If vec.Z Then knn.trainInput.Add(pt)
                    End If
                Next
            End If
            If knn.queries.Count = 0 Then Exit Sub

            knn.Run(src)

            dst2.SetTo(0)
            dist.inPoint1 = knn.queries(0)
            For i = 0 To knn.trainInput.Count - 1
                Dim pt = New cv.Point2f(knn.trainInput(i).X, knn.trainInput(i).Y)
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Red)
                dist.inPoint2 = knn.trainInput(i)
                dist.Run(src)
                SetTrueText("depth=" + CStr(knn.trainInput(i).Z) + vbCrLf + "dist=" + Format(dist.distance, fmt0), pt)
            Next
            For i = 0 To knn.queries.Count - 1
                Dim pt = New cv.Point2f(knn.queries(i).X, knn.queries(i).Y)
                For j = 0 To Math.Min(2, knn.trainInput.Count) - 1
                    Dim index = knn.result(i, j)
                    Dim nn = New cv.Point2f(knn.trainInput(index).X, knn.trainInput(index).Y)
                    DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Yellow)
                    vbc.DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
                    Dim midPt = New cv.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                    SetTrueText(CStr(j), midPt)
                    SetTrueText("depth=" + CStr(knn.queries(i).Z), pt)
                Next
            Next
        End Sub
    End Class








    Public Class NR_KNN_N4BasicsTest : Inherits TaskParent
        Dim knn As New KNN_N4Basics
        Dim dist As New Distance_Point4D
        Dim random As New Random_Basics4D
        Public Sub New()
            labels(2) = "Red=TrainingData, yellow = queries, text shows Euclidean distance to that point from query point"
            OptionParent.FindSlider("Random Pixel Count").Value = 5
            desc = "Validate that knn works with random 3D points in the image.  Find the nearest requested neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeat Then
                random.Run(src)
                knn.trainInput = New List(Of cv.Vec4f)(random.PointList)
                knn.queries.Clear()
                knn.queries.Add(New cv.Vec4f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height), msRNG.Next(0, dst2.Height)))
            End If

            knn.Run(src)

            dst2.SetTo(0)
            dist.inPoint1 = knn.queries(0)
            For i = 0 To knn.trainInput.Count - 1
                Dim pt = New cv.Point2f(knn.trainInput(i)(0), knn.trainInput(i)(1))
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Red)
                dist.inPoint2 = knn.trainInput(i)
                dist.Run(src)
                SetTrueText("dist=" + Format(dist.distance, fmt0), pt)
            Next
            For i = 0 To knn.queries.Count - 1
                Dim pt = New cv.Point2f(knn.queries(i)(0), knn.queries(i)(1))
                For j = knn.result.GetLowerBound(1) To knn.result.GetUpperBound(1)
                    Dim index = knn.result(i, j)
                    Dim nn = New cv.Point2f(knn.trainInput(index)(0), knn.trainInput(index)(1))
                    DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Yellow)
                    vbc.DrawLine(dst2, pt, nn, task.highlight)
                    Dim midPt = New cv.Point2f((pt.X + nn.X) / 2, (pt.Y + nn.Y) / 2)
                    SetTrueText(CStr(j), midPt)
                Next
            Next
        End Sub
    End Class






    Public Class NR_KNN_Farthest : Inherits TaskParent
        Public knn As New KNN_Basics
        Public lpFar As lpData
        Public Sub New()
            labels = {"", "", "Lines connecting pairs that are farthest.", "Training Input which is also query input and longest line"}
            desc = "Use KNN to find the farthest point from each query point."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If task.heartBeat Then
                    Static random As New Random_Basics
                    random.Run(src)
                    knn.trainInput = New List(Of cv.Point2f)(random.PointList)
                    knn.queries = New List(Of cv.Point2f)(knn.trainInput)
                End If
            End If

            knn.Run(src)

            dst2.SetTo(0)
            dst3.SetTo(0)
            Dim farthest = New List(Of lpData)
            Dim distances As New List(Of Single)
            For i = 0 To knn.result.GetUpperBound(0) - 1
                Dim farIndex = knn.result(i, knn.result.GetUpperBound(1))
                Dim lp = New lpData(knn.queries(i), knn.trainInput(farIndex))
                DrawCircle(dst2, lp.p1, task.DotSize + 4, cv.Scalar.Yellow)
                DrawCircle(dst2, lp.p2, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, lp.p1, lp.p2, cv.Scalar.Yellow)
                farthest.Add(lp)
                distances.Add(lp.p1.DistanceTo(lp.p2))
            Next

            For Each pt In knn.queries
                DrawCircle(dst3, pt, task.DotSize + 4, cv.Scalar.Red)
            Next

            Dim maxIndex = distances.IndexOf(distances.Max())
            lpFar = farthest(maxIndex)
            vbc.DrawLine(dst3, lpFar.p1, lpFar.p2, white)
        End Sub
    End Class







    Public Class NR_KNN_NNBasicsTest : Inherits TaskParent
        Dim knn As New KNN_NNBasics
        Public Sub New()
            labels(2) = "Highlight color (Yellow) is query.  The red dots are the training set."
            desc = "Test the use of the general form KNN_BasicsN algorithm"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.heartBeat Then
                knn.trainInput.Clear()
                For i = 0 To knn.options.numPoints - 1
                    For j = 0 To knn.options.knnDimension - 1
                        knn.trainInput.Add(msRNG.Next(dst2.Height))
                    Next
                Next

                knn.queries.Clear()
                For j = 0 To knn.options.knnDimension - 1
                    knn.queries.Add(msRNG.Next(dst2.Height))
                Next
            End If

            knn.Run(src)
            dst2.SetTo(0)
            For i = 0 To knn.trainInput.Count - 1 Step knn.options.knnDimension
                Dim pt = New cv.Point2f(knn.trainInput(i), knn.trainInput(i + 1))
                DrawCircle(dst2, pt, task.DotSize, cv.Scalar.Red)
            Next
            For i = 0 To knn.queries.Count - 1 Step knn.options.knnDimension
                Dim pt = New cv.Point2f(knn.queries(i), knn.queries(i + 1))
                Dim index = knn.result(i, 0)
                Dim nn = New cv.Point2f(knn.trainInput(index * knn.options.knnDimension), knn.trainInput(index * knn.options.knnDimension + 1))
                DrawCircle(dst2, pt, task.DotSize + 1, task.highlight)
                vbc.DrawLine(dst2, pt, nn, task.highlight)
            Next
            If standaloneTest() Then
                SetTrueText("Results are easily verified for the 2-dimensional case.  For higher dimension, " + vbCrLf +
                        "the results may appear incorrect because the higher dimensions are projected into " + vbCrLf +
                        "a 2-dimensional presentation.", 3)
            End If
        End Sub
    End Class






    Public Class KNN_NNBasics : Inherits TaskParent
        Implements IDisposable
        Public knn As cv.ML.KNearest
        Public trainInput As New List(Of Single) ' put training data here
        Public queries As New List(Of Single) ' put Query data here
        Public result(,) As Integer ' Get results here...
        Dim messageSent As Boolean
        Public neighbors As New cv.Mat
        Public options As New Options_KNN
        Public Sub New()
            knn = cv.ML.KNearest.Create()
            desc = "Generalize the use knn with X input points.  Find the nearest requested neighbors."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim responseList As IEnumerable(Of Integer) = Enumerable.Range(0, 10).Select(Function(x) x)
            If standaloneTest() Then
                SetTrueText("There is no output for the " + traceName + " algorithm when run standaloneTest().  Use the " + traceName + "_Test algorithm")
                Exit Sub
            End If

            If options.knnDimension = 0 Then
                If messageSent = False Then MessageBox.Show("The KNN dimension needs to be set for the general purpose KNN_Basics to start")
                Exit Sub
            End If

            Dim qRows = CInt(queries.Count / options.knnDimension)
            If qRows = 0 Then
                SetTrueText("There were no queries provided.  There is nothing to do...")
                Exit Sub
            End If

            Dim queryMat = cv.Mat.FromPixelData(qRows, options.knnDimension, cv.MatType.CV_32F, queries.ToArray)

            Dim trainData = cv.Mat.FromPixelData(trainInput.Count \ options.knnDimension,
                                              options.knnDimension, cv.MatType.CV_32F, trainInput.ToArray)
            Dim response = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S,
                                             Enumerable.Range(start:=0, trainData.Rows).ToArray)

            knn.Train(trainData, cv.ML.SampleTypes.RowSample, response)
            Dim dm = trainInput.Count
            knn.FindNearest(queryMat, dm, New cv.Mat, neighbors)

            ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
            For i = 0 To neighbors.Rows - 1
                For j = 0 To neighbors.Cols - 1
                    Dim test = neighbors.Get(Of Single)(i, j)
                    If test < trainInput.Count And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
                Next
            Next
        End Sub
        Protected Overrides Sub Finalize()
            If knn IsNot Nothing Then knn.Dispose()
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
                If task.heartBeat Then
                    random.Run(src)
                    knn.trainInput = New List(Of cv.Point2f)(random.PointList)
                    random.Run(src)
                    queries = New List(Of cv.Point2f)(random.PointList)
                End If
            End If

            If queries.Count = 0 Then
                SetTrueText("Place some input points in queries before starting the knn run.")
                Exit Sub
            End If

            knn.queries = queries
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
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Red)
            Next

            noMatch.Clear()
            matches.Clear()
            For i = 0 To queries.Count - 1
                Dim pt = queries(i)
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
                If nearest(i) = -1 Then
                    noMatch.Add(pt)
                Else
                    If nearest(i) < knn.trainInput.Count Then ' there seems like a boundary condition when there is only 1 traininput...
                        Dim nn = knn.trainInput(nearest(i))
                        matches.Add(New lpData(pt, nn))
                        vbc.DrawLine(dst2, nn, pt, white)
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
        Dim perif As New FCS_Periphery
        Public Sub New()
            desc = "Use the feature points on the periphery and find the points farthest from each."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Static pairList As New List(Of (cv.Point2f, cv.Point2f))

            perif.Run(src)
            dst3 = perif.dst3
            If task.features.Count = 0 Then Exit Sub

            knn.queries = perif.ptOutside
            knn.trainInput = knn.queries
            knn.Run(emptyMat)

            dst2 = src
            For i = 0 To knn.result.GetUpperBound(0)
                Dim lp = New lpData(knn.queries(knn.result(i, knn.queries.Count - 1)),
                                knn.queries(knn.result(i, 0)))
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth)
            Next

            labels(2) = "There were " + CStr(task.features.Count) + " features and " + CStr(knn.queries.Count) + " were on the periphery."
        End Sub
    End Class





    Public Class KNN_NormalizedBasics : Inherits TaskParent
        Public knn2 As New KNN_NNBasics
        Public queryInput As New List(Of Single)
        Public trainInput As New List(Of Single)
        Public result(,) As Integer ' Get results here...
        Public options As New Options_KNN
        Public Sub New()
            desc = "Default normalized KNN with dimension N"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim qRows = queryInput.Count \ options.knnDimension
            Dim queryData = cv.Mat.FromPixelData(qRows, options.knnDimension, cv.MatType.CV_32F, queryInput.ToArray)
            Dim queryMat As cv.Mat = queryData.Clone
            cv.Cv2.Normalize(queryData, queryMat, 0, 1, cv.NormTypes.L2)

            Dim tRows = trainInput.Count \ options.knnDimension
            Dim trainData = cv.Mat.FromPixelData(tRows, options.knnDimension, cv.MatType.CV_32F, trainInput.ToArray())
            cv.Cv2.Normalize(trainData, trainData, 0, 1, cv.NormTypes.L2)
            Dim response As cv.Mat = cv.Mat.FromPixelData(trainData.Rows, 1, cv.MatType.CV_32S,
                                  Enumerable.Range(start:=0, trainData.Rows).ToArray)

            knn2.trainInput = trainInput
            knn2.queries = queryInput
            knn2.Run(src)
            Dim neighbors As cv.Mat = knn2.neighbors
            ReDim result(neighbors.Rows - 1, neighbors.Cols - 1)
            For i = 0 To neighbors.Rows - 1
                For j = 0 To neighbors.Cols - 1
                    Dim test = neighbors.Get(Of Single)(i, j)
                    If test < trainInput.Count And test >= 0 Then result(i, j) = neighbors.Get(Of Single)(i, j)
                Next
            Next

            If standalone Then SetTrueText("Runs but no output when run standalone.")
        End Sub
    End Class





    Public Class NR_KNN_NormalizedTestDim2 : Inherits TaskParent
        Public knn As New KNN_NormalizedBasics
        Dim random As New Random_Basics
        Public trainInput As New List(Of cv.Point2f)
        Public queryInput As New List(Of cv.Point2f)
        Public Sub New()
            OptionParent.FindSlider("Random Pixel Count").Value = 20
            OptionParent.FindSlider("KNN Dimension").Value = 2
            desc = "Test KNN_NormalizedBasics with random 2D points in the image.  Find the nearest requested neighbors."
        End Sub
        Public Sub displayResults()
            dst2.SetTo(0)
            For i = 0 To queryInput.Count - 1
                Dim pt = queryInput(i)
                Dim index = knn.result(i, 0)
                Dim nn = trainInput(index)
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
            Next

            For Each pt In trainInput
                DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If task.heartBeat Then
                    queryInput.Clear()
                    trainInput.Clear()
                    random.Run(src)
                    For Each pt In random.PointList
                        If trainInput.Count < 10 Then
                            trainInput.Add(pt)
                        Else
                            queryInput.Add(pt)
                        End If
                    Next
                End If
            End If

            knn.trainInput.Clear()
            For Each pt In trainInput
                knn.trainInput.Add(pt.X)
                knn.trainInput.Add(pt.Y)
            Next

            knn.queryInput.Clear()
            For Each pt In queryInput
                knn.queryInput.Add(pt.X)
                knn.queryInput.Add(pt.Y)
            Next

            knn.Run(src)
            displayResults()

            labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
        End Sub
    End Class





    Public Class NR_KNN_NormalizedTestDim3 : Inherits TaskParent
        Public knn As New KNN_NormalizedBasics
        Dim random As New Random_Basics3D
        Public trainInput As New List(Of cv.Point3f)
        Public queryInput As New List(Of cv.Point3f)
        Public Sub New()
            OptionParent.FindSlider("Random Pixel Count").Value = 20
            OptionParent.FindSlider("KNN Dimension").Value = 3
            desc = "Test KNN_NormalizedBasics with random 3D points.  Find the nearest neighbors."
        End Sub
        Public Sub displayResults()
            dst2.SetTo(0)
            For i = 0 To queryInput.Count - 1
                Dim pt = New cv.Point(queryInput(i).X, queryInput(i).Y)
                Dim index = knn.result(i, 0)
                Dim nn = New cv.Point(trainInput(index).X, trainInput(index).Y)
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
            Next

            For Each pt In trainInput
                Dim p1 = New cv.Point(pt.X, pt.Y)
                DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If task.heartBeat Then
                    queryInput.Clear()
                    trainInput.Clear()
                    random.Run(src)
                    For Each pt In random.PointList
                        If trainInput.Count < 10 Then
                            trainInput.Add(pt)
                        Else
                            queryInput.Add(pt)
                        End If
                    Next
                End If
            End If

            knn.trainInput.Clear()
            For Each pt In trainInput
                knn.trainInput.Add(pt.X)
                knn.trainInput.Add(pt.Y)
                knn.trainInput.Add(pt.Z)
            Next

            knn.queryInput.Clear()
            For Each pt In queryInput
                knn.queryInput.Add(pt.X)
                knn.queryInput.Add(pt.Y)
                knn.queryInput.Add(pt.Z)
            Next

            knn.Run(src)
            displayResults()

            labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
        End Sub
    End Class





    Public Class NR_KNN_NormalizedTestDim4 : Inherits TaskParent
        Public knn As New KNN_NormalizedBasics
        Dim random As New Random_Basics4D
        Public trainInput As New List(Of cv.Vec4f)
        Public queryInput As New List(Of cv.Vec4f)
        Public Sub New()
            OptionParent.FindSlider("Random Pixel Count").Value = 20
            OptionParent.FindSlider("KNN Dimension").Value = 4
            desc = "Test KNN_NormalizedBasics with random 4D points.  Find the nearest neighbors."
        End Sub
        Public Sub displayResults()
            dst2.SetTo(0)
            For i = 0 To queryInput.Count - 1
                Dim pt = New cv.Point(queryInput(i).Item(0), queryInput(i).Item(1))
                Dim index = knn.result(i, 0)
                Dim nn = New cv.Point(trainInput(index).Item(0), trainInput(index).Item(1))
                DrawCircle(dst2, pt, task.DotSize + 4, cv.Scalar.Yellow)
                vbc.DrawLine(dst2, pt, nn, cv.Scalar.Yellow)
            Next

            For Each pt In trainInput
                Dim p1 = New cv.Point(pt.Item(0), pt.Item(1))
                DrawCircle(dst2, p1, task.DotSize + 2, cv.Scalar.Red)
            Next
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then
                If task.heartBeat Then
                    queryInput.Clear()
                    trainInput.Clear()
                    random.Run(src)
                    For Each pt In random.PointList
                        If trainInput.Count < 10 Then
                            trainInput.Add(pt)
                        Else
                            queryInput.Add(pt)
                        End If
                    Next
                End If
            End If

            knn.trainInput.Clear()
            For Each pt In trainInput
                knn.trainInput.Add(pt.Item(0))
                knn.trainInput.Add(pt.Item(1))
                knn.trainInput.Add(pt.Item(2))
                knn.trainInput.Add(pt.Item(3))
            Next

            knn.queryInput.Clear()
            For Each pt In queryInput
                knn.queryInput.Add(pt.Item(0))
                knn.queryInput.Add(pt.Item(1))
                knn.queryInput.Add(pt.Item(2))
                knn.queryInput.Add(pt.Item(3))
            Next

            knn.Run(src)
            displayResults()

            labels(2) = "Top " + CStr(trainInput.Count) + " best matches. Red=TrainInput, yellow = queryInput"
        End Sub
    End Class



    Public Class KNN_MinDistance : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public inputPoints As New List(Of cv.Point2f)
        Public outputPoints2f As New List(Of cv.Point2f)
        Public outputPoints As New List(Of cv.Point)
        Dim options As New Options_Features
        Dim feat As New Feature_General
        Public Sub New()
            If standalone Then task.fOptions.FeatureMethod.SelectedItem = "AGAST"
            desc = "Enforce a minimum distance to the next feature threshold"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            feat.Run(task.grayStable)
            labels = feat.labels

            If task.features.Count = 0 Then Exit Sub
            If standalone Then inputPoints = task.features

            knn.queries = New List(Of cv.Point2f)(inputPoints)
            knn.trainInput = knn.queries
            knn.Run(src)

            dst3.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst3, pt, task.DotSize, cv.Scalar.White)
            Next
            labels(3) = "There were " + CStr(inputPoints.Count) + " points in the input"

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
                DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.White)
                outputPoints.Add(pt)
                outputPoints2f.Add(pt)
            Next
            labels(2) = "After filtering for min distance = " + CStr(options.minDistance) + " there are " +
                    CStr(knn.queries.Count) + " points"
        End Sub
    End Class

End Namespace